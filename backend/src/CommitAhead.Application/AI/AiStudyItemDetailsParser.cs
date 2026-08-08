using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.AI;

/// <summary>
/// Thin AI-specific wrapper over the neutral <see cref="StudyItemDetailsJsonParser"/> — wraps a
/// parse/validation failure as <see cref="AiResponseValidationException"/>, since this is parsing
/// an AI-proposed StudyItemProposal, not a user-finalised one (compare
/// ApplyAnalysisDraftUseCase, which wraps the same neutral failure as
/// ApplyAnalysisDraftValidationException instead).
/// </summary>
internal static class AiStudyItemDetailsParser
{
    public static StudyItemDetails Parse(StudyItemCategory category, string detailsJson)
    {
        try
        {
            return StudyItemDetailsJsonParser.Parse(category, detailsJson);
        }
        catch (StudyItemDetailsPayloadException ex)
        {
            throw new AiResponseValidationException(ex.Message);
        }
    }
}
