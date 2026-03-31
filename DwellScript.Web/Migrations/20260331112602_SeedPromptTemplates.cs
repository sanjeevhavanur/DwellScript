using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DwellScript.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedPromptTemplates : Migration
    {
        private const string GenerationSystemPrompt =
            "You are an expert real estate copywriter specializing in rental property listings. " +
            "You write compelling, accurate, and platform-optimized copy that attracts qualified tenants. " +
            "You strictly follow Fair Housing laws and never include discriminatory language.";

        private const string FullGenerationPrompt =
            "Generate rental listing copy for the following property.\n\n" +
            "Property Details:\n" +
            "- Address: {Address}, {City}, {State} {Zip}\n" +
            "- Property Type: {PropertyType}\n" +
            "- Bedrooms: {Bedrooms} | Bathrooms: {Bathrooms}\n" +
            "- Square Footage: {SquareFootage}\n" +
            "- Monthly Rent: ${MonthlyRent}/mo\n" +
            "- Pet Policy: {PetPolicy}\n" +
            "- Parking: {Parking}\n" +
            "- Amenities: {Amenities}\n" +
            "- Target Platforms: {Platforms}\n" +
            "- Additional Notes: {Notes}" +
            "{RefinementInstruction}\n\n" +
            "Return your response in this exact XML format with no additional text before or after:\n\n" +
            "<outputs>\n" +
            "  <ltr>2-3 paragraph long-term rental listing optimized for Zillow/Apartments.com. " +
            "Professional tone, highlight key features and location benefits, end with a clear call to action.</ltr>\n" +
            "  <str>Short-term rental listing for Airbnb/VRBO. Engaging and vivid, 2-3 paragraphs, " +
            "highlight lifestyle appeal and unique features, friendly tone.</str>\n" +
            "  <social>Social media caption for Instagram/Facebook. Energetic tone, 150-200 words, " +
            "highlight the most eye-catching feature, end with 5-7 relevant hashtags.</social>\n" +
            "  <headlines>\n" +
            "    <headline>Catchy listing headline variant 1 (under 10 words)</headline>\n" +
            "    <headline>Catchy listing headline variant 2 (under 10 words)</headline>\n" +
            "    <headline>Catchy listing headline variant 3 (under 10 words)</headline>\n" +
            "  </headlines>\n" +
            "</outputs>";

        private const string SectionRegenPrompt =
            "Regenerate only the {Section} section of a rental listing.\n\n" +
            "Property Details:\n" +
            "- Address: {Address}, {City}, {State}\n" +
            "- Property Type: {PropertyType}\n" +
            "- Bedrooms: {Bedrooms} | Bathrooms: {Bathrooms}\n" +
            "- Monthly Rent: ${MonthlyRent}/mo\n" +
            "- Amenities: {Amenities}\n\n" +
            "{CurrentCopy}" +
            "{RefinementInstruction}\n\n" +
            "Return only the regenerated section in its XML tag, with no additional text:\n" +
            "- For LTR: <ltr>...</ltr>\n" +
            "- For STR: <str>...</str>\n" +
            "- For Social: <social>...</social>\n" +
            "- For Headlines: <headlines><headline>...</headline><headline>...</headline><headline>...</headline></headlines>";

        private const string AnalysisSystemPrompt =
            "You are an expert real estate consultant and rental market analyst. " +
            "You diagnose why rental listings underperform and provide specific, actionable recommendations. " +
            "You analyze copy quality, pricing, amenities, and market positioning. " +
            "Always respond with valid JSON only — no markdown, no commentary outside the JSON.";

        private const string VacancyAnalysisPrompt =
            "Analyze this underperforming rental listing and identify the top reasons it may not be attracting tenants.\n\n" +
            "Property Details:\n" +
            "- Address: {Address}, {City}, {State}\n" +
            "- Type: {PropertyType} | {Bedrooms} bed / {Bathrooms} bath\n" +
            "- Square Footage: {SquareFootage} sqft\n" +
            "- Monthly Rent: ${MonthlyRent}/mo\n" +
            "- Pet Policy: {PetPolicy}\n" +
            "- Parking: {Parking}\n" +
            "- Amenities: {Amenities}\n" +
            "- Days on Market: {DaysOnMarket}\n\n" +
            "Current LTR Listing Copy:\n{CurrentLtr}\n\n" +
            "Current STR Listing Copy:\n{CurrentStr}\n\n" +
            "Additional Context from Landlord:\n{Context}\n\n" +
            "Respond ONLY with a JSON object in this exact format (no markdown, no extra text):\n" +
            "{\n" +
            "  \"score\": <integer 0-100 representing overall listing health>,\n" +
            "  \"insights\": [\n" +
            "    {\n" +
            "      \"title\": \"<short issue title, max 8 words>\",\n" +
            "      \"detail\": \"<specific, actionable explanation in 1-2 sentences>\",\n" +
            "      \"severity\": \"<high|medium|low>\",\n" +
            "      \"suggestedFix\": \"<one concise instruction for rewriting the relevant section, e.g. 'Add in-unit laundry and rooftop deck to the amenities list in the LTR copy'>\",\n" +
            "      \"section\": \"<ltr|str|social|headlines — which section of the listing this fix applies to, or null if not applicable>\"\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "Rules:\n" +
            "- Return 3-5 insights, ranked from most to least impactful\n" +
            "- Score 80-100 = healthy, 60-79 = needs improvement, below 60 = critical issues\n" +
            "- Be specific — reference actual content from the listing copy when relevant\n" +
            "- Set section to null for issues not tied to a specific listing section (e.g. pricing, amenities gaps)\n" +
            "- Do not invent problems that aren't supported by the data provided";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PromptTemplates",
                columns: new[] { "Key", "PromptText", "SystemPrompt", "IsActive", "Version", "UpdatedAt" },
                values: new object[,]
                {
                    {
                        "FULL_GENERATION",
                        FullGenerationPrompt,
                        GenerationSystemPrompt,
                        true,
                        1,
                        new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        "SECTION_REGEN",
                        SectionRegenPrompt,
                        GenerationSystemPrompt,
                        true,
                        1,
                        new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        "VACANCY_ANALYSIS",
                        VacancyAnalysisPrompt,
                        AnalysisSystemPrompt,
                        true,
                        2,
                        new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PromptTemplates",
                keyColumn: "Key",
                keyValues: new object[] { "FULL_GENERATION", "SECTION_REGEN", "VACANCY_ANALYSIS" });
        }
    }
}
