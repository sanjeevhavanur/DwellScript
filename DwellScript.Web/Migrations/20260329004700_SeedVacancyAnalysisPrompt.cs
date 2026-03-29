using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DwellScript.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedVacancyAnalysisPrompt : Migration
    {
        private const string SystemPrompt =
            "You are an expert real estate consultant and rental market analyst. " +
            "You diagnose why rental listings underperform and provide specific, actionable recommendations. " +
            "You analyze copy quality, pricing, amenities, and market positioning. " +
            "Always respond with valid JSON only — no markdown, no commentary outside the JSON.";

        private const string PromptText =
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
            "      \"severity\": \"<high|medium|low>\"\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "Rules:\n" +
            "- Return 3-5 insights, ranked from most to least impactful\n" +
            "- Score 80-100 = healthy, 60-79 = needs improvement, below 60 = critical issues\n" +
            "- Be specific — reference actual content from the listing copy when relevant\n" +
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
                        "VACANCY_ANALYSIS",
                        PromptText,
                        SystemPrompt,
                        true,
                        1,
                        new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PromptTemplates",
                keyColumn: "Key",
                keyValues: new object[] { "VACANCY_ANALYSIS" });
        }
    }
}
