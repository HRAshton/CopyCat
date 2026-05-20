using CopyCat.Domain.Messages;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Evaluates filter definitions against normalized messages.
/// </summary>
public interface IFilterEvaluator
{
    /// <summary>
    /// Evaluates the specified message.
    /// </summary>
    /// <param name="message">The message to evaluate.</param>
    /// <param name="filterSet">The filter set definition.</param>
    /// <returns>The evaluation result.</returns>
    FilterDecision Evaluate(NormalizedTelegramMessage message, FilterSetDefinition filterSet);
}
