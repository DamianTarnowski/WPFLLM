using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Reflection;
using WPFLLM.Services;
using WPFLLM.Models;

namespace WPFLLM.Tests.Unit;

/// <summary>
/// Tests for DocumentAnalysisService parsing logic.
/// Tests the response parsing without needing actual LLM calls.
/// </summary>
[TestClass]
public class DocumentAnalysisParsingTests
{
    private MethodInfo _parseMethod = null!;
    private MethodInfo _parseBulletPointsMethod = null!;
    private MethodInfo _parseIntentsMethod = null!;
    private MethodInfo _parseRedFlagsMethod = null!;
    private MethodInfo _parseComplianceMethod = null!;

    [TestInitialize]
    public void Setup()
    {
        var type = typeof(DocumentAnalysisService);
        _parseMethod = type.GetMethod("ParseAnalysisResponse", BindingFlags.NonPublic | BindingFlags.Static)!;
        _parseBulletPointsMethod = type.GetMethod("ParseBulletPoints", BindingFlags.NonPublic | BindingFlags.Static)!;
        _parseIntentsMethod = type.GetMethod("ParseIntents", BindingFlags.NonPublic | BindingFlags.Static)!;
        _parseRedFlagsMethod = type.GetMethod("ParseRedFlags", BindingFlags.NonPublic | BindingFlags.Static)!;
        _parseComplianceMethod = type.GetMethod("ParseCompliance", BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    #region Bullet Points Parsing

    [TestMethod]
    public void ParseBulletPoints_StandardFormat_ShouldExtractPoints()
    {
        var content = """
            - Point one
            - Point two
            - Point three
            """;

        var result = (List<string>)_parseBulletPointsMethod.Invoke(null, new object[] { content })!;

        result.Should().HaveCount(3);
        result[0].Trim().Should().Be("Point one");
        result[1].Trim().Should().Be("Point two");
        result[2].Trim().Should().Be("Point three");
    }

    [TestMethod]
    public void ParseBulletPoints_AsteriskFormat_ShouldExtractPoints()
    {
        var content = """
            * First item
            * Second item
            """;

        var result = (List<string>)_parseBulletPointsMethod.Invoke(null, new object[] { content })!;

        result.Should().HaveCount(2);
        result[0].Trim().Should().Be("First item");
    }

    [TestMethod]
    public void ParseBulletPoints_EmptyContent_ShouldReturnEmpty()
    {
        var content = "";

        var result = (List<string>)_parseBulletPointsMethod.Invoke(null, new object[] { content })!;

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void ParseBulletPoints_WhitespaceOnly_ShouldReturnEmpty()
    {
        var content = "   \n   \n   ";

        var result = (List<string>)_parseBulletPointsMethod.Invoke(null, new object[] { content })!;

        result.Should().BeEmpty();
    }

    #endregion

    #region Intent Parsing

    [TestMethod]
    public void ParseIntents_StandardFormat_ShouldExtractIntents()
    {
        var content = """
            - Intent: Request Information | Confidence: high | Evidence: "Can you tell me about..."
            - Intent: Complaint | Confidence: medium | Evidence: "I'm not happy with..."
            """;

        var result = (List<DetectedIntent>)_parseIntentsMethod.Invoke(null, new object[] { content })!;

        result.Should().HaveCount(2);
        result[0].Intent.Should().Be("Request Information");
        result[0].Confidence.Should().Be(0.9);
        result[0].Evidence.Should().Be("Can you tell me about...");
    }

    [TestMethod]
    public void ParseIntents_HighConfidence_ShouldReturn09()
    {
        var content = "- Intent: Test | Confidence: high | Evidence: \"test\"";

        var result = (List<DetectedIntent>)_parseIntentsMethod.Invoke(null, new object[] { content })!;

        result[0].Confidence.Should().Be(0.9);
    }

    [TestMethod]
    public void ParseIntents_MediumConfidence_ShouldReturn06()
    {
        var content = "- Intent: Test | Confidence: medium | Evidence: \"test\"";

        var result = (List<DetectedIntent>)_parseIntentsMethod.Invoke(null, new object[] { content })!;

        result[0].Confidence.Should().Be(0.6);
    }

    [TestMethod]
    public void ParseIntents_LowConfidence_ShouldReturn03()
    {
        var content = "- Intent: Test | Confidence: low | Evidence: \"test\"";

        var result = (List<DetectedIntent>)_parseIntentsMethod.Invoke(null, new object[] { content })!;

        result[0].Confidence.Should().Be(0.3);
    }

    [TestMethod]
    public void ParseIntents_NoIntents_ShouldReturnEmpty()
    {
        var content = "No intents detected in this document.";

        var result = (List<DetectedIntent>)_parseIntentsMethod.Invoke(null, new object[] { content })!;

        result.Should().BeEmpty();
    }

    #endregion

    #region Red Flags Parsing

    [TestMethod]
    public void ParseRedFlags_StandardFormat_ShouldExtractFlags()
    {
        var content = """
            - Severity: high | Issue: Customer was interrupted | Quote: "Let me finish" | Recommendation: Allow customer to speak
            """;

        var result = (List<RedFlag>)_parseRedFlagsMethod.Invoke(null, new object[] { content })!;

        result.Should().HaveCount(1);
        result[0].Severity.Should().Be(RedFlagSeverity.High);
        result[0].Description.Should().Be("Customer was interrupted");
        result[0].Quote.Should().Be("Let me finish");
        result[0].Recommendation.Should().Be("Allow customer to speak");
    }

    [TestMethod]
    public void ParseRedFlags_CriticalSeverity_ShouldParseCritical()
    {
        var content = "- Severity: critical | Issue: Security breach | Quote: \"password\" | Recommendation: Escalate";

        var result = (List<RedFlag>)_parseRedFlagsMethod.Invoke(null, new object[] { content })!;

        result[0].Severity.Should().Be(RedFlagSeverity.Critical);
    }

    [TestMethod]
    public void ParseRedFlags_MediumSeverity_ShouldParseMedium()
    {
        var content = "- Severity: medium | Issue: Tone issue | Quote: \"test\" | Recommendation: Review";

        var result = (List<RedFlag>)_parseRedFlagsMethod.Invoke(null, new object[] { content })!;

        result[0].Severity.Should().Be(RedFlagSeverity.Medium);
    }

    [TestMethod]
    public void ParseRedFlags_LowSeverity_ShouldParseLow()
    {
        var content = "- Severity: low | Issue: Minor issue | Quote: \"test\" | Recommendation: Note";

        var result = (List<RedFlag>)_parseRedFlagsMethod.Invoke(null, new object[] { content })!;

        result[0].Severity.Should().Be(RedFlagSeverity.Low);
    }

    [TestMethod]
    public void ParseRedFlags_NoRedFlagsMessage_ShouldReturnEmpty()
    {
        var content = "No red flags detected in this conversation.";

        var result = (List<RedFlag>)_parseRedFlagsMethod.Invoke(null, new object[] { content })!;

        result.Should().BeEmpty();
    }

    #endregion

    #region Compliance Parsing

    [TestMethod]
    public void ParseCompliance_CheckmarkFormat_ShouldParseMet()
    {
        var content = """
            - [✓] Greeting: Proper greeting provided
            - [✗] Privacy statement: Not mentioned
            """;

        var result = (List<ComplianceItem>)_parseComplianceMethod.Invoke(null, new object[] { content })!;

        result.Should().HaveCount(2);
        result[0].IsMet.Should().BeTrue();
        result[0].Requirement.Should().Be("Greeting");
        result[0].Details.Should().Be("Proper greeting provided");
        result[1].IsMet.Should().BeFalse();
    }

    [TestMethod]
    public void ParseCompliance_XFormat_ShouldParseMet()
    {
        var content = "- [x] Requirement: Details here";

        var result = (List<ComplianceItem>)_parseComplianceMethod.Invoke(null, new object[] { content })!;

        result[0].IsMet.Should().BeTrue();
    }

    [TestMethod]
    public void ParseCompliance_NoColonSeparator_ShouldHandleGracefully()
    {
        var content = "- [✓] Simple requirement without details";

        var result = (List<ComplianceItem>)_parseComplianceMethod.Invoke(null, new object[] { content })!;

        result[0].Requirement.Should().Be("Simple requirement without details");
        result[0].Details.Should().BeEmpty();
    }

    #endregion

    #region Full Response Parsing

    [TestMethod]
    public void ParseAnalysisResponse_FullResponse_ShouldParseAllSections()
    {
        var response = """
            ## SUMMARY
            This is a test summary of the document content.

            ## KEY POINTS
            - First key point
            - Second key point

            ## DETECTED INTENTS
            - Intent: Information Request | Confidence: high | Evidence: "please tell me"

            ## RED FLAGS
            No red flags detected

            ## COMPLIANCE CHECKLIST
            - [✓] Greeting: Present
            - [✗] Privacy: Missing

            ## SUGGESTED RESPONSE
            Thank you for your inquiry. Here is the response.
            """;

        var result = new DocumentAnalysisResult();
        _parseMethod.Invoke(null, new object[] { response, result });

        result.Summary.Should().Contain("test summary");
        result.KeyPoints.Should().HaveCount(2);
        result.DetectedIntents.Should().HaveCount(1);
        result.RedFlags.Should().BeEmpty();
        result.ComplianceChecklist.Should().HaveCount(2);
        result.SuggestedResponse.Should().Contain("Thank you");
    }

    [TestMethod]
    public void ParseAnalysisResponse_EmptyResponse_ShouldNotThrow()
    {
        var response = "";
        var result = new DocumentAnalysisResult();

        var action = () => _parseMethod.Invoke(null, new object[] { response, result });

        action.Should().NotThrow();
    }

    [TestMethod]
    public void ParseAnalysisResponse_MalformedResponse_ShouldHandleGracefully()
    {
        var response = "This is not a properly formatted response with no sections.";
        var result = new DocumentAnalysisResult();

        var action = () => _parseMethod.Invoke(null, new object[] { response, result });

        action.Should().NotThrow();
    }

    #endregion
}
