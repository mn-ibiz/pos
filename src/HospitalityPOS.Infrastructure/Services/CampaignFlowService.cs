using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for automated marketing campaign flows.
/// </summary>
public class CampaignFlowService : ICampaignFlowService, ICampaignFlowTriggerService
{
    private readonly POSDbContext _context;
    private readonly ILogger<CampaignFlowService> _logger;
    private readonly ISmsService _smsService;

    public CampaignFlowService(
        POSDbContext context,
        ILogger<CampaignFlowService> logger,
        ISmsService smsService)
    {
        _context = context;
        _logger = logger;
        _smsService = smsService;
    }

    #region Flow Management

    public async Task<List<CampaignFlowDto>> GetAllFlowsAsync(int? storeId = null)
    {
        var query = _context.CampaignFlows
            .Include(f => f.Steps.Where(s => s.IsActive))
            .Include(f => f.Store)
            .Where(f => f.IsActive)
            .Where(f => !storeId.HasValue || f.StoreId == null || f.StoreId == storeId);

        var flows = await query.OrderBy(f => f.DisplayOrder).ToListAsync();
        return flows.Select(MapToFlowDto).ToList();
    }

    public async Task<CampaignFlowDto?> GetFlowAsync(int flowId)
    {
        var flow = await _context.CampaignFlows
            .Include(f => f.Steps.Where(s => s.IsActive).OrderBy(s => s.StepOrder))
            .Include(f => f.Store)
            .FirstOrDefaultAsync(f => f.Id == flowId && f.IsActive);

        return flow != null ? MapToFlowDto(flow) : null;
    }

    public async Task<List<CampaignFlowDto>> GetFlowsByTypeAsync(CampaignFlowType type, int? storeId = null)
    {
        var flows = await _context.CampaignFlows
            .Include(f => f.Steps.Where(s => s.IsActive))
            .Where(f => f.IsActive && f.Type == type)
            .Where(f => !storeId.HasValue || f.StoreId == null || f.StoreId == storeId)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();

        return flows.Select(MapToFlowDto).ToList();
    }

    public async Task<List<CampaignFlowDto>> GetFlowsByTriggerAsync(CampaignFlowTrigger trigger, int? storeId = null)
    {
        var flows = await _context.CampaignFlows
            .Include(f => f.Steps.Where(s => s.IsActive))
            .Where(f => f.IsActive && f.IsEnabled && f.Trigger == trigger)
            .Where(f => !storeId.HasValue || f.StoreId == null || f.StoreId == storeId)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();

        return flows.Select(MapToFlowDto).ToList();
    }

    public async Task<CampaignFlowDto> CreateFlowAsync(CreateCampaignFlowRequest request)
    {
        var flow = new CampaignFlow
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Trigger = request.Trigger,
            TriggerDaysOffset = request.TriggerDaysOffset,
            InactivityDaysThreshold = request.InactivityDaysThreshold,
            StoreId = request.StoreId,
            MinimumTier = request.MinimumTier,
            MaxEnrollmentsPerMember = request.MaxEnrollmentsPerMember,
            CooldownDays = request.CooldownDays,
            IsEnabled = true,
            IsActive = true
        };

        _context.CampaignFlows.Add(flow);
        await _context.SaveChangesAsync();

        // Add steps
        int stepOrder = 1;
        foreach (var stepRequest in request.Steps)
        {
            var step = CreateStepFromRequest(flow.Id, stepOrder++, stepRequest);
            _context.CampaignFlowSteps.Add(step);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created campaign flow {FlowId}: {FlowName}", flow.Id, flow.Name);
        return await GetFlowAsync(flow.Id) ?? throw new InvalidOperationException("Flow not found after creation");
    }

    public async Task<CampaignFlowDto> UpdateFlowAsync(int flowId, CreateCampaignFlowRequest request)
    {
        var flow = await _context.CampaignFlows.FindAsync(flowId)
            ?? throw new InvalidOperationException($"Flow {flowId} not found");

        flow.Name = request.Name;
        flow.Description = request.Description;
        flow.Type = request.Type;
        flow.Trigger = request.Trigger;
        flow.TriggerDaysOffset = request.TriggerDaysOffset;
        flow.InactivityDaysThreshold = request.InactivityDaysThreshold;
        flow.StoreId = request.StoreId;
        flow.MinimumTier = request.MinimumTier;
        flow.MaxEnrollmentsPerMember = request.MaxEnrollmentsPerMember;
        flow.CooldownDays = request.CooldownDays;
        flow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetFlowAsync(flowId) ?? throw new InvalidOperationException("Flow not found after update");
    }

    public async Task ActivateFlowAsync(int flowId)
    {
        var flow = await _context.CampaignFlows.FindAsync(flowId)
            ?? throw new InvalidOperationException($"Flow {flowId} not found");

        flow.IsEnabled = true;
        flow.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated campaign flow {FlowId}", flowId);
    }

    public async Task DeactivateFlowAsync(int flowId)
    {
        var flow = await _context.CampaignFlows.FindAsync(flowId)
            ?? throw new InvalidOperationException($"Flow {flowId} not found");

        flow.IsEnabled = false;
        flow.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated campaign flow {FlowId}", flowId);
    }

    public async Task DeleteFlowAsync(int flowId)
    {
        var flow = await _context.CampaignFlows.FindAsync(flowId)
            ?? throw new InvalidOperationException($"Flow {flowId} not found");

        flow.IsActive = false;
        flow.IsEnabled = false;
        flow.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted campaign flow {FlowId}", flowId);
    }

    #endregion

    #region Step Management

    public async Task<CampaignFlowStepDto> AddStepAsync(int flowId, CreateCampaignFlowStepRequest request)
    {
        var maxOrder = await _context.CampaignFlowSteps
            .Where(s => s.FlowId == flowId && s.IsActive)
            .MaxAsync(s => (int?)s.StepOrder) ?? 0;

        var step = CreateStepFromRequest(flowId, maxOrder + 1, request);
        _context.CampaignFlowSteps.Add(step);
        await _context.SaveChangesAsync();

        return MapToStepDto(step);
    }

    public async Task<CampaignFlowStepDto> UpdateStepAsync(int stepId, CreateCampaignFlowStepRequest request)
    {
        var step = await _context.CampaignFlowSteps.FindAsync(stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found");

        step.Name = request.Name;
        step.Description = request.Description;
        step.DelayDays = request.DelayDays;
        step.DelayHours = request.DelayHours;
        step.PreferredSendHour = request.PreferredSendHour;
        step.Channel = request.Channel;
        step.EmailTemplateId = request.EmailTemplateId;
        step.SmsTemplateId = request.SmsTemplateId;
        step.Subject = request.Subject;
        step.Content = request.Content;
        step.BonusPointsToAward = request.BonusPointsToAward;
        step.DiscountPercent = request.DiscountPercent;
        step.DiscountAmount = request.DiscountAmount;
        step.DiscountValidityDays = request.DiscountValidityDays;
        step.ConditionType = request.ConditionType;
        step.ConditionValue = request.ConditionValue;
        step.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToStepDto(step);
    }

    public async Task RemoveStepAsync(int stepId)
    {
        var step = await _context.CampaignFlowSteps.FindAsync(stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found");

        step.IsActive = false;
        step.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ReorderStepsAsync(int flowId, List<int> stepIds)
    {
        var steps = await _context.CampaignFlowSteps
            .Where(s => s.FlowId == flowId && stepIds.Contains(s.Id))
            .ToListAsync();

        for (int i = 0; i < stepIds.Count; i++)
        {
            var step = steps.FirstOrDefault(s => s.Id == stepIds[i]);
            if (step != null)
            {
                step.StepOrder = i + 1;
                step.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task EnableStepAsync(int stepId)
    {
        var step = await _context.CampaignFlowSteps.FindAsync(stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found");

        step.IsEnabled = true;
        step.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DisableStepAsync(int stepId)
    {
        var step = await _context.CampaignFlowSteps.FindAsync(stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found");

        step.IsEnabled = false;
        step.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private CampaignFlowStep CreateStepFromRequest(int flowId, int stepOrder, CreateCampaignFlowStepRequest request)
    {
        return new CampaignFlowStep
        {
            FlowId = flowId,
            StepOrder = stepOrder,
            Name = request.Name,
            Description = request.Description,
            DelayDays = request.DelayDays,
            DelayHours = request.DelayHours,
            PreferredSendHour = request.PreferredSendHour,
            Channel = request.Channel,
            EmailTemplateId = request.EmailTemplateId,
            SmsTemplateId = request.SmsTemplateId,
            Subject = request.Subject,
            Content = request.Content,
            BonusPointsToAward = request.BonusPointsToAward,
            DiscountPercent = request.DiscountPercent,
            DiscountAmount = request.DiscountAmount,
            DiscountValidityDays = request.DiscountValidityDays,
            ConditionType = request.ConditionType,
            ConditionValue = request.ConditionValue,
            IsEnabled = true,
            IsActive = true
        };
    }

    #endregion

    #region Enrollment

    public async Task<EnrollmentResult> EnrollMemberAsync(EnrollMemberRequest request)
    {
        var flow = await _context.CampaignFlows
            .Include(f => f.Steps.Where(s => s.IsActive && s.IsEnabled).OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(f => f.Id == request.FlowId && f.IsActive && f.IsEnabled);

        if (flow == null)
        {
            return new EnrollmentResult { Success = false, Error = "Flow not found or inactive" };
        }

        if (!flow.Steps.Any())
        {
            return new EnrollmentResult { Success = false, Error = "Flow has no active steps" };
        }

        var member = await _context.LoyaltyMembers.FindAsync(request.MemberId);
        if (member == null)
        {
            return new EnrollmentResult { Success = false, Error = "Member not found" };
        }

        // Check tier requirement
        if (flow.MinimumTier.HasValue && member.Tier < flow.MinimumTier)
        {
            return new EnrollmentResult { Success = false, Error = "Member does not meet tier requirement" };
        }

        // Check existing enrollments
        var existingCount = await _context.MemberFlowEnrollments
            .CountAsync(e => e.MemberId == request.MemberId && e.FlowId == request.FlowId && e.IsActive);

        if (existingCount >= flow.MaxEnrollmentsPerMember)
        {
            return new EnrollmentResult { Success = false, Error = "Maximum enrollments reached" };
        }

        // Check cooldown
        if (flow.CooldownDays > 0)
        {
            var lastEnrollment = await _context.MemberFlowEnrollments
                .Where(e => e.MemberId == request.MemberId && e.FlowId == request.FlowId)
                .OrderByDescending(e => e.EnrolledAt)
                .FirstOrDefaultAsync();

            if (lastEnrollment != null)
            {
                var cooldownEnd = lastEnrollment.EnrolledAt.AddDays(flow.CooldownDays);
                if (DateTime.UtcNow < cooldownEnd)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Error = $"Cooldown period not expired. Next enrollment available: {cooldownEnd:d}"
                    };
                }
            }
        }

        // Create enrollment
        var triggerDate = request.TriggerDate ?? DateTime.UtcNow;
        var firstStep = flow.Steps.First();
        var firstStepSchedule = CalculateStepSchedule(triggerDate, firstStep);

        var enrollment = new MemberFlowEnrollment
        {
            MemberId = request.MemberId,
            FlowId = request.FlowId,
            CurrentStepIndex = 0,
            Status = FlowEnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow,
            TriggerDate = triggerDate,
            NextStepScheduledAt = firstStepSchedule,
            StoreId = request.StoreId,
            IsActive = true
        };

        _context.MemberFlowEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        // Create first step execution
        var execution = new FlowStepExecution
        {
            EnrollmentId = enrollment.Id,
            StepId = firstStep.Id,
            ScheduledAt = firstStepSchedule,
            Status = FlowStepExecutionStatus.Scheduled,
            Channel = firstStep.Channel,
            IsActive = true
        };

        _context.FlowStepExecutions.Add(execution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Enrolled member {MemberId} in flow {FlowId}", request.MemberId, request.FlowId);

        return new EnrollmentResult
        {
            Success = true,
            Enrollment = await GetEnrollmentDtoAsync(enrollment.Id),
            Message = $"Enrolled in {flow.Name}"
        };
    }

    public async Task<EnrollmentResult> TriggerFlowAsync(TriggerFlowRequest request)
    {
        var flows = await GetFlowsByTriggerAsync(request.Trigger, request.StoreId);
        if (!flows.Any())
        {
            return new EnrollmentResult { Success = false, Error = "No active flows for this trigger" };
        }

        var flow = flows.First();
        return await EnrollMemberAsync(new EnrollMemberRequest
        {
            MemberId = request.MemberId,
            FlowId = flow.Id,
            TriggerDate = request.TriggerDate,
            StoreId = request.StoreId
        });
    }

    public async Task PauseEnrollmentAsync(int enrollmentId, string? reason = null)
    {
        var enrollment = await _context.MemberFlowEnrollments.FindAsync(enrollmentId)
            ?? throw new InvalidOperationException($"Enrollment {enrollmentId} not found");

        enrollment.Status = FlowEnrollmentStatus.Paused;
        enrollment.PausedAt = DateTime.UtcNow;
        enrollment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Paused enrollment {EnrollmentId}", enrollmentId);
    }

    public async Task ResumeEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _context.MemberFlowEnrollments.FindAsync(enrollmentId)
            ?? throw new InvalidOperationException($"Enrollment {enrollmentId} not found");

        if (enrollment.Status != FlowEnrollmentStatus.Paused)
        {
            throw new InvalidOperationException("Enrollment is not paused");
        }

        enrollment.Status = FlowEnrollmentStatus.Active;
        enrollment.PausedAt = null;
        enrollment.UpdatedAt = DateTime.UtcNow;

        // Reschedule next step if needed
        if (enrollment.NextStepScheduledAt.HasValue && enrollment.NextStepScheduledAt < DateTime.UtcNow)
        {
            enrollment.NextStepScheduledAt = DateTime.UtcNow.AddMinutes(15);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Resumed enrollment {EnrollmentId}", enrollmentId);
    }

    public async Task CancelEnrollmentAsync(int enrollmentId, string reason)
    {
        var enrollment = await _context.MemberFlowEnrollments.FindAsync(enrollmentId)
            ?? throw new InvalidOperationException($"Enrollment {enrollmentId} not found");

        enrollment.Status = FlowEnrollmentStatus.Cancelled;
        enrollment.CancelledAt = DateTime.UtcNow;
        enrollment.CancellationReason = reason;
        enrollment.UpdatedAt = DateTime.UtcNow;

        // Cancel pending executions
        var pendingExecutions = await _context.FlowStepExecutions
            .Where(e => e.EnrollmentId == enrollmentId && e.Status == FlowStepExecutionStatus.Scheduled)
            .ToListAsync();

        foreach (var exec in pendingExecutions)
        {
            exec.Status = FlowStepExecutionStatus.Cancelled;
            exec.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cancelled enrollment {EnrollmentId}: {Reason}", enrollmentId, reason);
    }

    public async Task<List<MemberFlowEnrollmentDto>> GetMemberEnrollmentsAsync(int memberId, bool activeOnly = false)
    {
        var query = _context.MemberFlowEnrollments
            .Include(e => e.Flow)
            .Include(e => e.Member)
            .Include(e => e.Store)
            .Where(e => e.MemberId == memberId && e.IsActive);

        if (activeOnly)
        {
            query = query.Where(e => e.Status == FlowEnrollmentStatus.Active);
        }

        var enrollments = await query.OrderByDescending(e => e.EnrolledAt).ToListAsync();
        return enrollments.Select(MapToEnrollmentDto).ToList();
    }

    public async Task<List<MemberFlowEnrollmentDto>> GetActiveEnrollmentsAsync(int flowId)
    {
        var enrollments = await _context.MemberFlowEnrollments
            .Include(e => e.Member)
            .Include(e => e.Flow)
            .Where(e => e.FlowId == flowId && e.IsActive && e.Status == FlowEnrollmentStatus.Active)
            .OrderBy(e => e.NextStepScheduledAt)
            .ToListAsync();

        return enrollments.Select(MapToEnrollmentDto).ToList();
    }

    public async Task<List<FlowStepExecutionDto>> GetEnrollmentExecutionsAsync(int enrollmentId)
    {
        var executions = await _context.FlowStepExecutions
            .Include(e => e.Step)
            .Where(e => e.EnrollmentId == enrollmentId)
            .OrderBy(e => e.ScheduledAt)
            .ToListAsync();

        return executions.Select(MapToExecutionDto).ToList();
    }

    #endregion

    #region Execution

    public async Task<FlowProcessingResult> ProcessScheduledStepsAsync()
    {
        var result = new FlowProcessingResult();

        try
        {
            var now = DateTime.UtcNow;

            // Get scheduled executions that are due
            var dueExecutions = await _context.FlowStepExecutions
                .Include(e => e.Enrollment)
                    .ThenInclude(en => en.Flow)
                        .ThenInclude(f => f.Steps)
                .Include(e => e.Enrollment)
                    .ThenInclude(en => en.Member)
                .Include(e => e.Step)
                .Where(e => e.IsActive &&
                           e.Status == FlowStepExecutionStatus.Scheduled &&
                           e.ScheduledAt <= now &&
                           e.Enrollment.Status == FlowEnrollmentStatus.Active)
                .ToListAsync();

            foreach (var execution in dueExecutions)
            {
                result.StepsProcessed++;

                try
                {
                    var execResult = await ExecuteStepInternalAsync(execution);

                    if (execResult.Success)
                    {
                        result.StepsExecuted++;
                    }
                    else if (execResult.Skipped)
                    {
                        result.StepsSkipped++;
                    }
                    else
                    {
                        result.StepsFailed++;
                    }

                    // Check if enrollment is complete
                    if (execution.Enrollment.Status == FlowEnrollmentStatus.Completed)
                    {
                        result.EnrollmentsCompleted++;
                    }
                }
                catch (Exception ex)
                {
                    result.StepsFailed++;
                    result.Errors.Add($"Step {execution.Id}: {ex.Message}");
                    _logger.LogError(ex, "Error executing step {ExecutionId}", execution.Id);
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Fatal error: {ex.Message}");
            _logger.LogError(ex, "Error in ProcessScheduledStepsAsync");
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<StepExecutionResult> ExecuteStepAsync(int enrollmentId, int stepId)
    {
        var execution = await _context.FlowStepExecutions
            .Include(e => e.Enrollment)
                .ThenInclude(en => en.Flow)
                    .ThenInclude(f => f.Steps)
            .Include(e => e.Enrollment)
                .ThenInclude(en => en.Member)
            .Include(e => e.Step)
            .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId && e.StepId == stepId);

        if (execution == null)
        {
            // Create execution if it doesn't exist
            execution = new FlowStepExecution
            {
                EnrollmentId = enrollmentId,
                StepId = stepId,
                ScheduledAt = DateTime.UtcNow,
                Status = FlowStepExecutionStatus.Scheduled,
                IsActive = true
            };
            _context.FlowStepExecutions.Add(execution);
            await _context.SaveChangesAsync();

            execution = await _context.FlowStepExecutions
                .Include(e => e.Enrollment)
                    .ThenInclude(en => en.Flow)
                        .ThenInclude(f => f.Steps)
                .Include(e => e.Enrollment)
                    .ThenInclude(en => en.Member)
                .Include(e => e.Step)
                .FirstAsync(e => e.Id == execution.Id);
        }

        return await ExecuteStepInternalAsync(execution);
    }

    private async Task<StepExecutionResult> ExecuteStepInternalAsync(FlowStepExecution execution)
    {
        var result = new StepExecutionResult();
        var step = execution.Step;
        var enrollment = execution.Enrollment;
        var member = enrollment.Member;

        execution.Status = FlowStepExecutionStatus.Executing;
        await _context.SaveChangesAsync();

        try
        {
            // Check condition
            if (step.ConditionType != StepConditionType.None)
            {
                var conditionMet = await EvaluateConditionAsync(step, enrollment, member);
                if (!conditionMet)
                {
                    execution.Status = FlowStepExecutionStatus.Skipped;
                    execution.SkipReason = $"Condition not met: {step.ConditionType}";
                    execution.ExecutedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await AdvanceToNextStepAsync(enrollment);

                    return new StepExecutionResult
                    {
                        Success = true,
                        Skipped = true,
                        SkipReason = execution.SkipReason,
                        Execution = MapToExecutionDto(execution)
                    };
                }
            }

            // Render content
            var renderedContent = await RenderTemplateAsync(step.Content ?? "", member);
            var renderedSubject = await RenderTemplateAsync(step.Subject ?? "", member);

            execution.RenderedContent = renderedContent;
            execution.RenderedSubject = renderedSubject;

            // Send message
            switch (step.Channel)
            {
                case CampaignChannel.SMS:
                    if (!string.IsNullOrEmpty(member.PhoneNumber))
                    {
                        var smsResult = await _smsService.SendSmsAsync(member.PhoneNumber, renderedContent);
                        execution.ExternalMessageId = smsResult.MessageId;
                        execution.Delivered = smsResult.Success;
                        if (!smsResult.Success)
                        {
                            execution.ErrorMessage = smsResult.ErrorMessage;
                        }
                    }
                    else
                    {
                        execution.ErrorMessage = "No phone number";
                    }
                    break;

                case CampaignChannel.Email:
                    // Email sending would be implemented here
                    execution.Delivered = true; // Placeholder
                    break;

                case CampaignChannel.Push:
                case CampaignChannel.InApp:
                    // Push/InApp notifications would be implemented here
                    execution.Delivered = true; // Placeholder
                    break;
            }

            // Award bonus points if configured
            if (step.BonusPointsToAward.HasValue && step.BonusPointsToAward > 0)
            {
                member.PointsBalance += step.BonusPointsToAward.Value;
                member.TotalPointsEarned += step.BonusPointsToAward.Value;
                execution.PointsAwarded = step.BonusPointsToAward.Value;
                result.PointsAwarded = step.BonusPointsToAward.Value;
            }

            // Generate discount code if configured
            if (step.DiscountPercent.HasValue || step.DiscountAmount.HasValue)
            {
                var discountCode = GenerateDiscountCode();
                execution.DiscountCode = discountCode;
                result.DiscountCode = discountCode;
                // Would create actual coupon record here
            }

            execution.Status = FlowStepExecutionStatus.Executed;
            execution.ExecutedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Advance to next step
            await AdvanceToNextStepAsync(enrollment);

            result.Success = true;
            result.Execution = MapToExecutionDto(execution);
            result.Message = $"Executed step: {step.Name}";

            _logger.LogInformation("Executed step {StepId} for enrollment {EnrollmentId}",
                step.Id, enrollment.Id);
        }
        catch (Exception ex)
        {
            execution.Status = FlowStepExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.RetryCount++;
            execution.ExecutedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            result.Success = false;
            result.Error = ex.Message;

            _logger.LogError(ex, "Failed to execute step {StepId}", step.Id);
        }

        return result;
    }

    private async Task AdvanceToNextStepAsync(MemberFlowEnrollment enrollment)
    {
        var steps = enrollment.Flow.Steps
            .Where(s => s.IsActive && s.IsEnabled)
            .OrderBy(s => s.StepOrder)
            .ToList();

        var nextIndex = enrollment.CurrentStepIndex + 1;

        if (nextIndex >= steps.Count)
        {
            // Flow complete
            enrollment.Status = FlowEnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
            enrollment.NextStepScheduledAt = null;
        }
        else
        {
            enrollment.CurrentStepIndex = nextIndex;
            var nextStep = steps[nextIndex];
            var nextSchedule = CalculateStepSchedule(DateTime.UtcNow, nextStep);

            enrollment.NextStepScheduledAt = nextSchedule;

            // Create next execution
            var execution = new FlowStepExecution
            {
                EnrollmentId = enrollment.Id,
                StepId = nextStep.Id,
                ScheduledAt = nextSchedule,
                Status = FlowStepExecutionStatus.Scheduled,
                Channel = nextStep.Channel,
                IsActive = true
            };
            _context.FlowStepExecutions.Add(execution);
        }

        enrollment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task SkipStepAsync(int enrollmentId, int stepId, string reason)
    {
        var execution = await _context.FlowStepExecutions
            .Include(e => e.Enrollment)
                .ThenInclude(en => en.Flow)
                    .ThenInclude(f => f.Steps)
            .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId && e.StepId == stepId);

        if (execution == null)
            throw new InvalidOperationException("Execution not found");

        execution.Status = FlowStepExecutionStatus.Skipped;
        execution.SkipReason = reason;
        execution.ExecutedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await AdvanceToNextStepAsync(execution.Enrollment);
    }

    public async Task<StepExecutionResult> RetryStepAsync(int executionId)
    {
        var execution = await _context.FlowStepExecutions
            .Include(e => e.Enrollment)
                .ThenInclude(en => en.Flow)
                    .ThenInclude(f => f.Steps)
            .Include(e => e.Enrollment)
                .ThenInclude(en => en.Member)
            .Include(e => e.Step)
            .FirstOrDefaultAsync(e => e.Id == executionId);

        if (execution == null)
            throw new InvalidOperationException("Execution not found");

        if (execution.Status != FlowStepExecutionStatus.Failed)
            throw new InvalidOperationException("Only failed executions can be retried");

        execution.Status = FlowStepExecutionStatus.Scheduled;
        execution.ScheduledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await ExecuteStepInternalAsync(execution);
    }

    private DateTime CalculateStepSchedule(DateTime baseTime, CampaignFlowStep step)
    {
        var scheduled = baseTime
            .AddDays(step.DelayDays)
            .AddHours(step.DelayHours);

        if (step.PreferredSendHour.HasValue)
        {
            scheduled = new DateTime(
                scheduled.Year, scheduled.Month, scheduled.Day,
                step.PreferredSendHour.Value, 0, 0, DateTimeKind.Utc);

            if (scheduled < baseTime)
                scheduled = scheduled.AddDays(1);
        }

        return scheduled;
    }

    private async Task<bool> EvaluateConditionAsync(CampaignFlowStep step, MemberFlowEnrollment enrollment, LoyaltyMember member)
    {
        switch (step.ConditionType)
        {
            case StepConditionType.NoPurchaseSinceEnrollment:
                var hasPurchase = await _context.Receipts
                    .AnyAsync(r => r.LoyaltyMemberId == member.Id && r.CreatedAt >= enrollment.EnrolledAt);
                return !hasPurchase;

            case StepConditionType.MemberStillInactive:
                var lastReceipt = await _context.Receipts
                    .Where(r => r.LoyaltyMemberId == member.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
                return lastReceipt == null || lastReceipt.CreatedAt < enrollment.EnrolledAt;

            case StepConditionType.TierIs:
                return step.ConditionValue != null &&
                       member.Tier.ToString() == step.ConditionValue;

            case StepConditionType.PointsAbove:
                return int.TryParse(step.ConditionValue, out var minPoints) &&
                       member.PointsBalance > minPoints;

            case StepConditionType.PointsBelow:
                return int.TryParse(step.ConditionValue, out var maxPoints) &&
                       member.PointsBalance < maxPoints;

            default:
                return true;
        }
    }

    private async Task<string> RenderTemplateAsync(string template, LoyaltyMember member)
    {
        var result = template
            .Replace(TemplateVariables.CustomerName, $"{member.FirstName} {member.LastName}".Trim())
            .Replace(TemplateVariables.FirstName, member.FirstName)
            .Replace(TemplateVariables.LastName, member.LastName ?? "")
            .Replace(TemplateVariables.PointsBalance, member.PointsBalance.ToString("N0"))
            .Replace(TemplateVariables.TierName, member.Tier.ToString())
            .Replace(TemplateVariables.MembershipNumber, member.MembershipNumber ?? member.Id.ToString());

        // Get tier multiplier
        var tier = await _context.LoyaltyTierConfigurations
            .FirstOrDefaultAsync(t => t.Tier == member.Tier && t.IsActive);

        if (tier != null)
        {
            result = result.Replace(TemplateVariables.TierMultiplier, tier.PointsMultiplier.ToString("0.#"));
        }

        // Get referral code
        var referralCode = await _context.ReferralCodes
            .FirstOrDefaultAsync(r => r.MemberId == member.Id && r.IsActive);

        if (referralCode != null)
        {
            result = result.Replace(TemplateVariables.ReferralCode, referralCode.Code);
        }

        return result;
    }

    private string GenerateDiscountCode()
    {
        return $"CAMP{DateTime.UtcNow:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    #endregion

    #region Trigger Service Implementation

    public async Task OnMemberEnrolledAsync(int memberId, int? storeId = null)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnEnrollment,
            StoreId = storeId
        });
    }

    public async Task OnBirthdayAsync(int memberId)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnBirthday,
            TriggerDate = DateTime.UtcNow
        });
    }

    public async Task OnAnniversaryAsync(int memberId)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnAnniversary,
            TriggerDate = DateTime.UtcNow
        });
    }

    public async Task OnPurchaseAsync(int memberId, int receiptId, decimal amount, int? storeId = null)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnPurchase,
            StoreId = storeId,
            Context = new Dictionary<string, object>
            {
                { "ReceiptId", receiptId },
                { "Amount", amount }
            }
        });
    }

    public async Task OnTierChangedAsync(int memberId, LoyaltyTier oldTier, LoyaltyTier newTier)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnTierChange,
            Context = new Dictionary<string, object>
            {
                { "OldTier", oldTier.ToString() },
                { "NewTier", newTier.ToString() }
            }
        });
    }

    public async Task OnPointsExpiryApproachingAsync(int memberId, int daysUntilExpiry, int expiringPoints)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnPointsExpiry,
            Context = new Dictionary<string, object>
            {
                { "DaysUntilExpiry", daysUntilExpiry },
                { "ExpiringPoints", expiringPoints }
            }
        });
    }

    public async Task OnInactivityDetectedAsync(int memberId, int daysSinceLastVisit)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnInactivity,
            Context = new Dictionary<string, object>
            {
                { "DaysSinceLastVisit", daysSinceLastVisit }
            }
        });
    }

    public async Task OnReferralCompleteAsync(int memberId, int referredMemberId)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnReferralComplete,
            Context = new Dictionary<string, object>
            {
                { "ReferredMemberId", referredMemberId }
            }
        });
    }

    public async Task OnChallengeCompleteAsync(int memberId, int challengeId)
    {
        await TriggerFlowAsync(new TriggerFlowRequest
        {
            MemberId = memberId,
            Trigger = CampaignFlowTrigger.OnChallengeComplete,
            Context = new Dictionary<string, object>
            {
                { "ChallengeId", challengeId }
            }
        });
    }

    #endregion

    #region Analytics

    public async Task<FlowAnalytics> GetFlowAnalyticsAsync(int flowId, DateTime from, DateTime to)
    {
        var flow = await _context.CampaignFlows
            .Include(f => f.Steps)
            .FirstOrDefaultAsync(f => f.Id == flowId);

        if (flow == null)
            throw new InvalidOperationException($"Flow {flowId} not found");

        var enrollments = await _context.MemberFlowEnrollments
            .Where(e => e.FlowId == flowId && e.EnrolledAt >= from && e.EnrolledAt <= to)
            .ToListAsync();

        var executions = await _context.FlowStepExecutions
            .Include(e => e.Step)
            .Where(e => e.Enrollment.FlowId == flowId &&
                       e.ExecutedAt >= from && e.ExecutedAt <= to)
            .ToListAsync();

        return new FlowAnalytics
        {
            FlowId = flowId,
            FlowName = flow.Name,
            FlowType = flow.Type,
            FromDate = from,
            ToDate = to,
            TotalEnrollments = enrollments.Count,
            ActiveEnrollments = enrollments.Count(e => e.Status == FlowEnrollmentStatus.Active),
            CompletedEnrollments = enrollments.Count(e => e.Status == FlowEnrollmentStatus.Completed),
            CancelledEnrollments = enrollments.Count(e => e.Status == FlowEnrollmentStatus.Cancelled),
            TotalMessagesent = executions.Count(e => e.Status == FlowStepExecutionStatus.Executed),
            TotalDelivered = executions.Count(e => e.Delivered == true),
            TotalOpened = executions.Count(e => e.Opened == true),
            TotalClicked = executions.Count(e => e.Clicked == true),
            TotalPointsAwarded = executions.Sum(e => e.PointsAwarded ?? 0),
            TotalDiscountCodesIssued = executions.Count(e => !string.IsNullOrEmpty(e.DiscountCode)),
            StepAnalytics = flow.Steps.Where(s => s.IsActive).Select(s => new StepAnalytics
            {
                StepId = s.Id,
                StepName = s.Name,
                StepOrder = s.StepOrder,
                Channel = s.Channel,
                Executions = executions.Count(e => e.StepId == s.Id),
                Delivered = executions.Count(e => e.StepId == s.Id && e.Delivered == true),
                Opened = executions.Count(e => e.StepId == s.Id && e.Opened == true),
                Clicked = executions.Count(e => e.StepId == s.Id && e.Clicked == true),
                Failed = executions.Count(e => e.StepId == s.Id && e.Status == FlowStepExecutionStatus.Failed),
                Skipped = executions.Count(e => e.StepId == s.Id && e.Status == FlowStepExecutionStatus.Skipped)
            }).ToList()
        };
    }

    public async Task<FlowPerformanceReport> GetPerformanceReportAsync(DateTime from, DateTime to)
    {
        var flows = await _context.CampaignFlows.Where(f => f.IsActive && f.IsEnabled).ToListAsync();

        var enrollments = await _context.MemberFlowEnrollments
            .Where(e => e.EnrolledAt >= from && e.EnrolledAt <= to)
            .ToListAsync();

        var executions = await _context.FlowStepExecutions
            .Where(e => e.ExecutedAt >= from && e.ExecutedAt <= to &&
                       e.Status == FlowStepExecutionStatus.Executed)
            .ToListAsync();

        return new FlowPerformanceReport
        {
            FromDate = from,
            ToDate = to,
            TotalActiveFlows = flows.Count,
            TotalEnrollments = enrollments.Count,
            TotalMessagesent = executions.Count,
            TotalDelivered = executions.Count(e => e.Delivered == true),
            OverallDeliveryRate = executions.Count > 0
                ? (decimal)executions.Count(e => e.Delivered == true) / executions.Count * 100
                : 0,
            OverallOpenRate = executions.Count(e => e.Delivered == true) > 0
                ? (decimal)executions.Count(e => e.Opened == true) / executions.Count(e => e.Delivered == true) * 100
                : 0,
            OverallClickRate = executions.Count(e => e.Opened == true) > 0
                ? (decimal)executions.Count(e => e.Clicked == true) / executions.Count(e => e.Opened == true) * 100
                : 0,
            EnrollmentsByType = enrollments
                .GroupBy(e => e.Flow?.Type ?? CampaignFlowType.Custom)
                .ToDictionary(g => g.Key, g => g.Count()),
            MessagesByChannel = executions
                .GroupBy(e => e.Channel)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    #endregion

    #region Configuration

    public async Task<CampaignFlowConfigurationDto> GetConfigurationAsync(int? storeId = null)
    {
        var config = await _context.CampaignFlowConfigurations
            .FirstOrDefaultAsync(c => c.IsActive && (c.StoreId == storeId || (storeId.HasValue && c.StoreId == null)));

        if (config == null)
        {
            return new CampaignFlowConfigurationDto
            {
                IsEnabled = true,
                EmailEnabled = true,
                SmsEnabled = true,
                MaxMessagesPerMemberPerDay = 3,
                QuietHoursStart = 21,
                QuietHoursEnd = 8,
                WinBackInactivityDays = 30,
                BirthdayFlowStartDays = 7,
                PointsExpiryNotifyDays = 30,
                MaxRetryAttempts = 3,
                RetryDelayMinutes = 15
            };
        }

        return MapToConfigurationDto(config);
    }

    public async Task<CampaignFlowConfigurationDto> UpdateConfigurationAsync(CampaignFlowConfigurationDto dto)
    {
        var config = await _context.CampaignFlowConfigurations.FindAsync(dto.Id);

        if (config == null)
        {
            config = new CampaignFlowConfiguration { IsActive = true };
            _context.CampaignFlowConfigurations.Add(config);
        }

        config.StoreId = dto.StoreId;
        config.IsEnabled = dto.IsEnabled;
        config.EmailEnabled = dto.EmailEnabled;
        config.SmsEnabled = dto.SmsEnabled;
        config.DefaultFromEmail = dto.DefaultFromEmail;
        config.DefaultFromName = dto.DefaultFromName;
        config.DefaultSmsFrom = dto.DefaultSmsFrom;
        config.MaxMessagesPerMemberPerDay = dto.MaxMessagesPerMemberPerDay;
        config.QuietHoursStart = dto.QuietHoursStart;
        config.QuietHoursEnd = dto.QuietHoursEnd;
        config.WinBackInactivityDays = dto.WinBackInactivityDays;
        config.BirthdayFlowStartDays = dto.BirthdayFlowStartDays;
        config.PointsExpiryNotifyDays = dto.PointsExpiryNotifyDays;
        config.MaxRetryAttempts = dto.MaxRetryAttempts;
        config.RetryDelayMinutes = dto.RetryDelayMinutes;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        dto.Id = config.Id;
        return dto;
    }

    #endregion

    #region Templates

    public async Task<List<EmailTemplateDto>> GetEmailTemplatesAsync(int? storeId = null)
    {
        return await _context.CampaignEmailTemplates
            .Where(t => t.IsActive)
            .Where(t => !storeId.HasValue || t.StoreId == null || t.StoreId == storeId)
            .OrderBy(t => t.Name)
            .Select(t => MapToEmailTemplateDto(t))
            .ToListAsync();
    }

    public async Task<EmailTemplateDto?> GetEmailTemplateAsync(int templateId)
    {
        var template = await _context.CampaignEmailTemplates.FindAsync(templateId);
        return template != null ? MapToEmailTemplateDto(template) : null;
    }

    public async Task<EmailTemplateDto> CreateEmailTemplateAsync(EmailTemplateDto dto)
    {
        var template = new CampaignEmailTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            Subject = dto.Subject,
            HtmlBody = dto.HtmlBody,
            TextBody = dto.TextBody,
            Category = dto.Category,
            StoreId = dto.StoreId,
            IsActive = true
        };

        _context.CampaignEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        dto.Id = template.Id;
        return dto;
    }

    public async Task<EmailTemplateDto> UpdateEmailTemplateAsync(EmailTemplateDto dto)
    {
        var template = await _context.CampaignEmailTemplates.FindAsync(dto.Id)
            ?? throw new InvalidOperationException($"Email template {dto.Id} not found");

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Subject = dto.Subject;
        template.HtmlBody = dto.HtmlBody;
        template.TextBody = dto.TextBody;
        template.Category = dto.Category;
        template.StoreId = dto.StoreId;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteEmailTemplateAsync(int templateId)
    {
        var template = await _context.CampaignEmailTemplates.FindAsync(templateId);
        if (template != null)
        {
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<SmsTemplateDto>> GetSmsTemplatesAsync(int? storeId = null)
    {
        return await _context.CampaignSmsTemplates
            .Where(t => t.IsActive)
            .Where(t => !storeId.HasValue || t.StoreId == null || t.StoreId == storeId)
            .OrderBy(t => t.Name)
            .Select(t => MapToSmsTemplateDto(t))
            .ToListAsync();
    }

    public async Task<SmsTemplateDto?> GetSmsTemplateAsync(int templateId)
    {
        var template = await _context.CampaignSmsTemplates.FindAsync(templateId);
        return template != null ? MapToSmsTemplateDto(template) : null;
    }

    public async Task<SmsTemplateDto> CreateSmsTemplateAsync(SmsTemplateDto dto)
    {
        var template = new CampaignSmsTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            Content = dto.Content,
            Category = dto.Category,
            StoreId = dto.StoreId,
            IsActive = true
        };

        _context.CampaignSmsTemplates.Add(template);
        await _context.SaveChangesAsync();

        dto.Id = template.Id;
        return dto;
    }

    public async Task<SmsTemplateDto> UpdateSmsTemplateAsync(SmsTemplateDto dto)
    {
        var template = await _context.CampaignSmsTemplates.FindAsync(dto.Id)
            ?? throw new InvalidOperationException($"SMS template {dto.Id} not found");

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Content = dto.Content;
        template.Category = dto.Category;
        template.StoreId = dto.StoreId;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteSmsTemplateAsync(int templateId)
    {
        var template = await _context.CampaignSmsTemplates.FindAsync(templateId);
        if (template != null)
        {
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string> PreviewTemplateAsync(string content, int? memberId = null)
    {
        if (memberId.HasValue)
        {
            var member = await _context.LoyaltyMembers.FindAsync(memberId.Value);
            if (member != null)
            {
                return await RenderTemplateAsync(content, member);
            }
        }

        // Preview with sample data
        return content
            .Replace(TemplateVariables.CustomerName, "John Smith")
            .Replace(TemplateVariables.FirstName, "John")
            .Replace(TemplateVariables.LastName, "Smith")
            .Replace(TemplateVariables.PointsBalance, "500")
            .Replace(TemplateVariables.TierName, "Gold")
            .Replace(TemplateVariables.TierMultiplier, "1.5")
            .Replace(TemplateVariables.MembershipNumber, "12345")
            .Replace(TemplateVariables.ReferralCode, "JOHN123");
    }

    #endregion

    #region Mapping Helpers

    private CampaignFlowDto MapToFlowDto(CampaignFlow flow)
    {
        return new CampaignFlowDto
        {
            Id = flow.Id,
            Name = flow.Name,
            Description = flow.Description,
            Type = flow.Type,
            Trigger = flow.Trigger,
            TriggerDaysOffset = flow.TriggerDaysOffset,
            InactivityDaysThreshold = flow.InactivityDaysThreshold,
            StoreId = flow.StoreId,
            StoreName = flow.Store?.Name,
            MinimumTier = flow.MinimumTier,
            IsEnabled = flow.IsEnabled,
            MaxEnrollmentsPerMember = flow.MaxEnrollmentsPerMember,
            CooldownDays = flow.CooldownDays,
            DisplayOrder = flow.DisplayOrder,
            StepCount = flow.Steps?.Count(s => s.IsActive) ?? 0,
            Steps = flow.Steps?.Where(s => s.IsActive).OrderBy(s => s.StepOrder).Select(MapToStepDto).ToList() ?? new(),
            CreatedAt = flow.CreatedAt,
            UpdatedAt = flow.UpdatedAt
        };
    }

    private CampaignFlowStepDto MapToStepDto(CampaignFlowStep step)
    {
        return new CampaignFlowStepDto
        {
            Id = step.Id,
            FlowId = step.FlowId,
            StepOrder = step.StepOrder,
            Name = step.Name,
            Description = step.Description,
            DelayDays = step.DelayDays,
            DelayHours = step.DelayHours,
            PreferredSendHour = step.PreferredSendHour,
            Channel = step.Channel,
            EmailTemplateId = step.EmailTemplateId,
            SmsTemplateId = step.SmsTemplateId,
            Subject = step.Subject,
            Content = step.Content,
            BonusPointsToAward = step.BonusPointsToAward,
            DiscountPercent = step.DiscountPercent,
            DiscountAmount = step.DiscountAmount,
            DiscountValidityDays = step.DiscountValidityDays,
            ConditionType = step.ConditionType,
            ConditionValue = step.ConditionValue,
            IsEnabled = step.IsEnabled
        };
    }

    private MemberFlowEnrollmentDto MapToEnrollmentDto(MemberFlowEnrollment enrollment)
    {
        return new MemberFlowEnrollmentDto
        {
            Id = enrollment.Id,
            MemberId = enrollment.MemberId,
            MemberName = enrollment.Member != null ? $"{enrollment.Member.FirstName} {enrollment.Member.LastName}".Trim() : "",
            MemberPhone = enrollment.Member?.PhoneNumber,
            MemberEmail = enrollment.Member?.Email,
            FlowId = enrollment.FlowId,
            FlowName = enrollment.Flow?.Name ?? "",
            FlowType = enrollment.Flow?.Type ?? CampaignFlowType.Custom,
            CurrentStepIndex = enrollment.CurrentStepIndex,
            TotalSteps = enrollment.Flow?.Steps?.Count(s => s.IsActive) ?? 0,
            Status = enrollment.Status,
            EnrolledAt = enrollment.EnrolledAt,
            TriggerDate = enrollment.TriggerDate,
            NextStepScheduledAt = enrollment.NextStepScheduledAt,
            CompletedAt = enrollment.CompletedAt,
            PausedAt = enrollment.PausedAt,
            CancelledAt = enrollment.CancelledAt,
            CancellationReason = enrollment.CancellationReason,
            StoreId = enrollment.StoreId,
            StoreName = enrollment.Store?.Name
        };
    }

    private async Task<MemberFlowEnrollmentDto?> GetEnrollmentDtoAsync(int enrollmentId)
    {
        var enrollment = await _context.MemberFlowEnrollments
            .Include(e => e.Member)
            .Include(e => e.Flow)
                .ThenInclude(f => f.Steps)
            .Include(e => e.Store)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        return enrollment != null ? MapToEnrollmentDto(enrollment) : null;
    }

    private FlowStepExecutionDto MapToExecutionDto(FlowStepExecution execution)
    {
        return new FlowStepExecutionDto
        {
            Id = execution.Id,
            EnrollmentId = execution.EnrollmentId,
            StepId = execution.StepId,
            StepName = execution.Step?.Name ?? "",
            StepOrder = execution.Step?.StepOrder ?? 0,
            ScheduledAt = execution.ScheduledAt,
            ExecutedAt = execution.ExecutedAt,
            Status = execution.Status,
            Channel = execution.Channel,
            ExternalMessageId = execution.ExternalMessageId,
            Delivered = execution.Delivered,
            DeliveredAt = execution.DeliveredAt,
            Opened = execution.Opened,
            OpenedAt = execution.OpenedAt,
            Clicked = execution.Clicked,
            ClickedAt = execution.ClickedAt,
            ErrorMessage = execution.ErrorMessage,
            RetryCount = execution.RetryCount,
            PointsAwarded = execution.PointsAwarded,
            DiscountCode = execution.DiscountCode,
            SkipReason = execution.SkipReason,
            RenderedSubject = execution.RenderedSubject
        };
    }

    private static EmailTemplateDto MapToEmailTemplateDto(CampaignEmailTemplate template)
    {
        return new CampaignEmailTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Subject = template.Subject,
            HtmlBody = template.HtmlBody,
            TextBody = template.TextBody,
            Category = template.Category,
            StoreId = template.StoreId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static SmsTemplateDto MapToSmsTemplateDto(CampaignSmsTemplate template)
    {
        return new CampaignSmsTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Content = template.Content,
            Category = template.Category,
            StoreId = template.StoreId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static CampaignFlowConfigurationDto MapToConfigurationDto(CampaignFlowConfiguration config)
    {
        return new CampaignFlowConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            IsEnabled = config.IsEnabled,
            EmailEnabled = config.EmailEnabled,
            SmsEnabled = config.SmsEnabled,
            DefaultFromEmail = config.DefaultFromEmail,
            DefaultFromName = config.DefaultFromName,
            DefaultSmsFrom = config.DefaultSmsFrom,
            MaxMessagesPerMemberPerDay = config.MaxMessagesPerMemberPerDay,
            QuietHoursStart = config.QuietHoursStart,
            QuietHoursEnd = config.QuietHoursEnd,
            WinBackInactivityDays = config.WinBackInactivityDays,
            BirthdayFlowStartDays = config.BirthdayFlowStartDays,
            PointsExpiryNotifyDays = config.PointsExpiryNotifyDays,
            MaxRetryAttempts = config.MaxRetryAttempts,
            RetryDelayMinutes = config.RetryDelayMinutes
        };
    }

    #endregion
}
