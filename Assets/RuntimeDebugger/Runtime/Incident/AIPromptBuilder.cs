using System;
using System.Collections.Generic;
using System.Text;

namespace RuntimeDebugger
{
    /// <summary>
    /// Structured AI analysis result.
    /// </summary>
    [Serializable]
    public class RootCauseHypothesis
    {
        public string Description;
        public string Confidence; // High/Medium/Low
        public List<string> SupportingEvidence;
    }

    [Serializable]
    public class AIResult
    {
        public string Summary;
        public List<string> Evidence;
        public List<RootCauseHypothesis> Hypotheses;
        public List<string> Unknowns;
        public List<string> VerificationSteps;
        public string RawResponse;

        public static AIResult ParseFromLLMResponse(string response)
        {
            var result = new AIResult
            {
                Summary = "",
                Evidence = new List<string>(),
                Hypotheses = new List<RootCauseHypothesis>(),
                Unknowns = new List<string>(),
                VerificationSteps = new List<string>(),
                RawResponse = response
            };

            if (string.IsNullOrEmpty(response))
                return result;

            // Simple section-based parsing
            string[] sections = response.Split(new[] { "\n---", "\n##", "\n[" }, StringSplitOptions.RemoveEmptyEntries);
            string currentSection = "";

            foreach (var line in response.Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Incident Summary:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "summary";
                    result.Summary = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                }
                else if (trimmed.StartsWith("Evidence:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "evidence";
                }
                else if (trimmed.StartsWith("Hypothes", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("Root Cause", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "hypotheses";
                }
                else if (trimmed.StartsWith("Unknowns:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "unknowns";
                }
                else if (trimmed.StartsWith("Verification:", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("Suggested Verification", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "verification";
                }
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    string item = trimmed.Substring(2).Trim();
                    switch (currentSection)
                    {
                        case "evidence": result.Evidence.Add(item); break;
                        case "unknowns": result.Unknowns.Add(item); break;
                        case "verification": result.VerificationSteps.Add(item); break;
                        case "hypotheses":
                            result.Hypotheses.Add(new RootCauseHypothesis
                            {
                                Description = item,
                                Confidence = "Medium",
                                SupportingEvidence = new List<string>()
                            });
                            break;
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Builds a structured prompt for LLM analysis of a runtime incident.
    /// </summary>
    public static class AIPromptBuilder
    {
        /// <summary>
        /// Build a complete prompt for LLM analysis.
        /// </summary>
        public static string BuildPrompt(RuntimeIncident incident, string codeContext = null)
        {
            var sb = new StringBuilder(8192);

            sb.AppendLine(DebugLocale.Get("ai.intro"));
            sb.AppendLine();
            sb.AppendLine(DebugLocale.Get("ai.task"));
            sb.AppendLine();
            sb.AppendLine("Output format (use exactly these sections):");
            sb.AppendLine();
            sb.AppendLine("Summary: <one-paragraph summary>");
            sb.AppendLine("Evidence:");
            sb.AppendLine("- <evidence item>");
            sb.AppendLine("Hypotheses:");
            sb.AppendLine("- <candidate root cause> (Confidence: High/Medium/Low)");
            sb.AppendLine("Unknowns:");
            sb.AppendLine("- <what remains uncertain>");
            sb.AppendLine("Verification:");
            sb.AppendLine("- <suggested verification step>");
            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("- Claims without runtime evidence must be marked as Hypothesis, not Root Cause.");
            sb.AppendLine("- Cite specific trace nodes, metrics, or lifecycle events as evidence.");
            sb.AppendLine("- If a race condition was detected (async task completed after owner destroyed), highlight it.");
            sb.AppendLine();

            // Runtime context
            sb.AppendLine("=== RUNTIME CONTEXT ===");
            sb.Append(IncidentContextBuilder.Build(incident));
            sb.AppendLine();

            // Code context (optional)
            if (!string.IsNullOrEmpty(codeContext))
            {
                sb.AppendLine("=== RELEVANT CODE ===");
                sb.AppendLine(codeContext);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Build a prompt and save to file for manual LLM submission.
        /// </summary>
        public static string BuildAndSavePrompt(RuntimeIncident incident, string outputDir)
        {
            string prompt = BuildPrompt(incident);
            string fileName = $"ai_prompt_{incident.Type}_{incident.TriggerTimestampMs}.txt";
            string filePath = System.IO.Path.Combine(outputDir, fileName);

            System.IO.Directory.CreateDirectory(outputDir);
            System.IO.File.WriteAllText(filePath, prompt);

            return filePath;
        }
    }
}
