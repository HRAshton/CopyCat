using System.Text.RegularExpressions;

using CopyCat.Domain.Enums;
using CopyCat.Domain.Messages;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Evaluates filter definitions against normalized messages.
/// </summary>
public sealed class FilterEvaluator : IFilterEvaluator
{
    /// <inheritdoc />
    public FilterDecision Evaluate(NormalizedTelegramMessage message, FilterSetDefinition filterSet)
    {
        List<string> trace = [];
        if (filterSet.Root is null)
        {
            bool accepted = filterSet.DefaultPolicy == MappingDefaultPolicy.Allow;
            trace.Add($"No filter tree configured. Default policy = {filterSet.DefaultPolicy}.");
            return new FilterDecision(
                accepted,
                null,
                trace,
                accepted ? "Accepted by default policy." : "Rejected by default policy.");
        }

        (bool passed, string? ruleId) = EvaluateNode(filterSet.Root, message, trace);

        return new FilterDecision(
            passed,
            ruleId,
            trace,
            passed ? "Accepted by filter." : "Rejected by filter.");
    }

    private static (bool Passed, string? RuleId) EvaluateNode(
        FilterNode node,
        NormalizedTelegramMessage message,
        List<string> trace)
    {
        return node switch
        {
            ConditionGroup group => ProcessConditionGroup(message, trace, group),
            HasTextCondition hasText => ProcessHasTextCondition(message, trace, hasText),
            HasAttachmentCondition hasAttachment => ProcessHasAttachmentCondition(message, trace, hasAttachment),
            ContainsWordsCondition containsWords => ProcessContainsWordsCondition(message, trace, containsWords),
            HasTelegramLinkCondition tgLink => ProcessHasTelegramLinkCondition(message, trace, tgLink),
            HasAnyUrlCondition anyUrl => ProcessHasAnyUrlCondition(message, trace, anyUrl),
            RegexCondition regex => ProcessRegexCondition(message, trace, regex),
            LengthCondition length => ProcessLengthCondition(message, trace, length),
            IsEditedCondition edited => ProcessIsEditedCondition(message, trace, edited),
            _ => ProcessUnknown(node, trace),
        };
    }

    private static (bool Passed, string? RuleId) ProcessConditionGroup(
        NormalizedTelegramMessage message,
        List<string> trace,
        ConditionGroup group)
    {
        (bool Passed, string? RuleId)[] childResults =
            group.Children.Select(child => EvaluateNode(child, message, trace)).ToArray();
        bool passed = group.Operator == LogicalOperator.And
            ? childResults.All(x => x.Passed)
            : childResults.Any(x => x.Passed);
        trace.Add($"Group {group.Operator} evaluated to {passed}.");
        return (passed, childResults.FirstOrDefault(x => x.Passed).RuleId ?? group.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessHasTextCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        HasTextCondition hasText)
    {
        bool hasMessageText = message.HasText;
        trace.Add($"Has text = {hasMessageText}; expected {hasText.Expected}.");
        return (hasMessageText == hasText.Expected, hasText.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessHasAttachmentCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        HasAttachmentCondition hasAttachment)
    {
        bool attachmentMatch = hasAttachment.Types.Count == 0
            ? message.AttachmentTypes.Count > 0
            : message.AttachmentTypes.Any(hasAttachment.Types.Contains);
        trace.Add($"Attachment match = {attachmentMatch}.");
        return (attachmentMatch, hasAttachment.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessContainsWordsCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        ContainsWordsCondition containsWords)
    {
        string haystack = containsWords.CaseSensitive
            ? message.NormalizedText
            : message.NormalizedText.ToLowerInvariant();
        IReadOnlyList<string> words = containsWords.CaseSensitive
            ? containsWords.Words
            : containsWords.Words.Select(x => x.ToLowerInvariant()).ToArray();
        string[] matches = words.Where(word => haystack.Contains(
                word,
                containsWords.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            .ToArray();
        bool wordsMatch = containsWords.MatchMode == MatchMode.All
            ? matches.Length == words.Count
            : matches.Length > 0;
        trace.Add($"Word condition matched {matches.Length} word(s).");
        return (containsWords.IsWhitelist ? wordsMatch : !wordsMatch, containsWords.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessHasTelegramLinkCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        HasTelegramLinkCondition tgLink)
    {
        bool hasTelegramLink = message.TelegramLinks.Count > 0;
        trace.Add($"Telegram link present = {hasTelegramLink}; expected {tgLink.Expected}.");
        return (hasTelegramLink == tgLink.Expected, tgLink.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessHasAnyUrlCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        HasAnyUrlCondition anyUrl)
    {
        bool hasAnyUrl = message.Links.Count > 0;
        trace.Add($"Any URL present = {hasAnyUrl}; expected {anyUrl.Expected}.");
        return (hasAnyUrl == anyUrl.Expected, anyUrl.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessRegexCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        RegexCondition regex)
    {
        string fieldValue = regex.Field switch
        {
            TextField.Text => message.Text ?? string.Empty,
            TextField.Caption => message.Caption ?? string.Empty,
            TextField.NormalizedText => message.NormalizedText,
            _ => $"{message.Text} {message.Caption} {message.NormalizedText}".Trim(),
        };
        bool regexMatch = Regex.IsMatch(
            fieldValue,
            regex.Pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        trace.Add($"Regex '{regex.Pattern}' match = {regexMatch}.");
        return (regexMatch, regex.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessLengthCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        LengthCondition length)
    {
        int size = (message.Text ?? message.Caption ?? string.Empty).Length;
        bool minOkay = !length.Minimum.HasValue || size >= length.Minimum.Value;
        bool maxOkay = !length.Maximum.HasValue || size <= length.Maximum.Value;
        trace.Add($"Length {size} within range = {minOkay && maxOkay}.");
        return (minOkay && maxOkay, length.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessIsEditedCondition(
        NormalizedTelegramMessage message,
        List<string> trace,
        IsEditedCondition edited)
    {
        bool isEdited = message.EditDate.HasValue;
        trace.Add($"Edited = {isEdited}; expected {edited.Expected}.");
        return (isEdited == edited.Expected, edited.RuleId);
    }

    private static (bool Passed, string? RuleId) ProcessUnknown(FilterNode node, List<string> trace)
    {
        trace.Add($"Unknown filter node {node.GetType().Name}.");
        return (false, node.RuleId);
    }
}
