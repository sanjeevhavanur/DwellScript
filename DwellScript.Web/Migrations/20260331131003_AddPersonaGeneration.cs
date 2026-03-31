using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DwellScript.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonaGeneration : Migration
    {
        /// <inheritdoc />
        private const string SystemPrompt =
            "You are an expert real estate copywriter specializing in rental property listings. " +
            "You write compelling, accurate, and platform-optimized copy that attracts qualified tenants. " +
            "You strictly follow Fair Housing laws and never include discriminatory language. " +
            "You emphasize property attributes and features — never targeting tenants by identity, " +
            "race, religion, national origin, sex, familial status, or disability.";

        private const string PersonaPrompt =
            "Write a long-term rental (LTR) listing for the following property, " +
            "with emphasis on features and attributes that appeal to the '{PersonaName}' lifestyle.\n\n" +
            "Property Details:\n" +
            "- Address: {Address}, {City}, {State}\n" +
            "- Type: {PropertyType} | {Bedrooms} bed / {Bathrooms} bath\n" +
            "- Square Footage: {SquareFootage}\n" +
            "- Monthly Rent: ${MonthlyRent}/mo\n" +
            "- Pet Policy: {PetPolicy}\n" +
            "- Parking: {Parking}\n" +
            "- Amenities: {Amenities}\n\n" +
            "Lifestyle emphasis — highlight these property attributes where applicable:\n" +
            "{Emphasis}\n\n" +
            "Instructions:\n" +
            "- Write 2-3 paragraphs of polished, professional LTR listing copy\n" +
            "- Emphasize the property FEATURES that align with the lifestyle (do NOT target or mention tenant characteristics)\n" +
            "- Professional tone, accurate to the property details provided\n" +
            "- End with a clear call to action\n" +
            "- Strictly follow Fair Housing laws: do not reference race, religion, national origin, sex, " +
            "familial status, disability, or any protected class\n" +
            "- Output the listing text only — no headings, no labels, no explanation" +
            "{RefinementInstruction}";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonaKey",
                table: "Generations",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "PromptTemplates",
                columns: new[] { "Key", "PromptText", "SystemPrompt", "IsActive", "Version", "UpdatedAt" },
                values: new object[,]
                {
                    {
                        "PERSONA_GENERATION",
                        PersonaPrompt,
                        SystemPrompt,
                        true,
                        1,
                        new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonaKey",
                table: "Generations");

            migrationBuilder.DeleteData(
                table: "PromptTemplates",
                keyColumn: "Key",
                keyValues: new object[] { "PERSONA_GENERATION" });
        }
    }
}
