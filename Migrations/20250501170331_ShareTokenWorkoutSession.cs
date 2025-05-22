using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class ShareTokenWorkoutSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MetricCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    ThresholdValue = table.Column<double>(type: "float", nullable: false),
                    ActualValue = table.Column<double>(type: "float", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WasAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcknowledgementNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WasEscalated = table.Column<bool>(type: "bit", nullable: false),
                    TimeToResolve = table.Column<TimeSpan>(type: "time", nullable: true),
                    TimeToAcknowledge = table.Column<TimeSpan>(type: "time", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertThreshold",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetricName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MetricCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarningThreshold = table.Column<double>(type: "float", nullable: false),
                    CriticalThreshold = table.Column<double>(type: "float", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    NotificationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EscalationMinutes = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertThreshold", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    EquipmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.EquipmentId);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseType",
                columns: table => new
                {
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Muscle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrimaryMuscles = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecondaryMuscles = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Equipment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsFromApi = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseType", x => x.ExerciseTypeId);
                });

            migrationBuilder.CreateTable(
                name: "GlossaryTerm",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Term = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTerm", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HelpCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpCategory_HelpCategory_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "HelpCategory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LoginHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingExerciseSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    ExerciseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    SelectedApiExerciseIndex = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingExerciseSelection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settype",
                columns: table => new
                {
                    SettypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settype", x => x.SettypeId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Alert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertThresholdId = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CurrentValue = table.Column<double>(type: "float", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcknowledgementNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alert", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alert_AlertThreshold_AlertThresholdId",
                        column: x => x.AlertThresholdId,
                        principalTable: "AlertThreshold",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsViewedByCoach = table.Column<bool>(type: "bit", nullable: false),
                    ViewedByCoachDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientActivities_AppUser_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientActivities_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ClientGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientGroups_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GlossaryTermRelatedTerms",
                columns: table => new
                {
                    GlossaryTermId = table.Column<int>(type: "int", nullable: false),
                    RelatedTermsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTermRelatedTerms", x => new { x.GlossaryTermId, x.RelatedTermsId });
                    table.ForeignKey(
                        name: "FK_GlossaryTermRelatedTerms_GlossaryTerm_GlossaryTermId",
                        column: x => x.GlossaryTermId,
                        principalTable: "GlossaryTerm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GlossaryTermRelatedTerms_GlossaryTerm_RelatedTermsId",
                        column: x => x.RelatedTermsId,
                        principalTable: "GlossaryTerm",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HelpArticle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HelpCategoryId = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    HasVideo = table.Column<bool>(type: "bit", nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrintable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpArticle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpArticle_HelpCategory_HelpCategoryId",
                        column: x => x.HelpCategoryId,
                        principalTable: "HelpCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientEquipments",
                columns: table => new
                {
                    ClientEquipmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    EquipmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientEquipments", x => x.ClientEquipmentId);
                    table.ForeignKey(
                        name: "FK_ClientEquipments_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientExerciseExclusions",
                columns: table => new
                {
                    ClientExerciseExclusionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByCoachId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientExerciseExclusions", x => x.ClientExerciseExclusionId);
                    table.ForeignKey(
                        name: "FK_ClientExerciseExclusions_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientExerciseExclusions_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientExerciseExclusions_User_CreatedByCoachId",
                        column: x => x.CreatedByCoachId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSubstitutions",
                columns: table => new
                {
                    ExerciseSubstitutionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrimaryExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    SubstituteExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    EquivalencePercentage = table.Column<int>(type: "int", nullable: false),
                    MovementPattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EquipmentRequired = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MusclesTargeted = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByCoachId = table.Column<int>(type: "int", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSubstitutions", x => x.ExerciseSubstitutionId);
                    table.ForeignKey(
                        name: "FK_ExerciseSubstitutions_ExerciseType_PrimaryExerciseTypeId",
                        column: x => x.PrimaryExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseSubstitutions_ExerciseType_SubstituteExerciseTypeId",
                        column: x => x.SubstituteExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseSubstitutions_User_CreatedByCoachId",
                        column: x => x.CreatedByCoachId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    FeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    AssignedToAdminId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicResponse = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrowserInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_Feedback_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    datetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    endtime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Session_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplate",
                columns: table => new
                {
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplate", x => x.WorkoutTemplateId);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplate_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_Alert_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alert",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CoachClientRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    InvitedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvitationExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientGroupId = table.Column<int>(type: "int", nullable: true),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AppUserId1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachClientRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AppUser_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AppUser_AppUserId1",
                        column: x => x.AppUserId1,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AppUser_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_ClientGroups_ClientGroupId",
                        column: x => x.ClientGroupId,
                        principalTable: "ClientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HelpArticleRelatedArticles",
                columns: table => new
                {
                    HelpArticleId = table.Column<int>(type: "int", nullable: false),
                    RelatedArticlesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpArticleRelatedArticles", x => new { x.HelpArticleId, x.RelatedArticlesId });
                    table.ForeignKey(
                        name: "FK_HelpArticleRelatedArticles_HelpArticle_HelpArticleId",
                        column: x => x.HelpArticleId,
                        principalTable: "HelpArticle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HelpArticleRelatedArticles_HelpArticle_RelatedArticlesId",
                        column: x => x.RelatedArticlesId,
                        principalTable: "HelpArticle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Set",
                columns: table => new
                {
                    SetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettypeId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    NumberReps = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Set", x => x.SetId);
                    table.ForeignKey(
                        name: "FK_Set_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Set_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Set_Settype_SettypeId",
                        column: x => x.SettypeId,
                        principalTable: "Settype",
                        principalColumn: "SettypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutFeedbacks",
                columns: table => new
                {
                    WorkoutFeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    FeedbackDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverallRating = table.Column<int>(type: "int", nullable: false),
                    DifficultyRating = table.Column<int>(type: "int", nullable: false),
                    EnergyLevel = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedAllExercises = table.Column<bool>(type: "bit", nullable: false),
                    IncompleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoachNotified = table.Column<bool>(type: "bit", nullable: false),
                    CoachViewed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutFeedbacks", x => x.WorkoutFeedbackId);
                    table.ForeignKey(
                        name: "FK_WorkoutFeedbacks_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutFeedbacks_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateExercise",
                columns: table => new
                {
                    WorkoutTemplateExerciseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: true),
                    Sets = table.Column<int>(type: "int", nullable: false),
                    MinReps = table.Column<int>(type: "int", nullable: false),
                    MaxReps = table.Column<int>(type: "int", nullable: false),
                    RestSeconds = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateExercise", x => x.WorkoutTemplateExerciseId);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercise_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentId");
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercise_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercise_WorkoutTemplate_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplate",
                        principalColumn: "WorkoutTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCoachCreated = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CustomCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeasurementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StartValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsVisibleToCoach = table.Column<bool>(type: "bit", nullable: false),
                    TrackingFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LastProgressUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionCriteria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientGoals_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientGroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientGroupId = table.Column<int>(type: "int", nullable: false),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientGroupMembers_ClientGroups_ClientGroupId",
                        column: x => x.ClientGroupId,
                        principalTable: "ClientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientGroupMembers_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachClientMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFromCoach = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachClientMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachClientMessages_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachClientPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    CanViewWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanModifyWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanEditWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanDeleteWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanViewPersonalInfo = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateGoals = table.Column<bool>(type: "bit", nullable: false),
                    CanViewReports = table.Column<bool>(type: "bit", nullable: false),
                    CanMessage = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateTemplates = table.Column<bool>(type: "bit", nullable: false),
                    CanAssignTemplates = table.Column<bool>(type: "bit", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachClientPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachClientPermissions_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVisibleToClient = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachNotes_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAssignments",
                columns: table => new
                {
                    TemplateAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: false),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    ClientGroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CoachNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ClientNotified = table.Column<bool>(type: "bit", nullable: false),
                    ClientRelationshipId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAssignments", x => x.TemplateAssignmentId);
                    table.ForeignKey(
                        name: "FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId",
                        column: x => x.ClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TemplateAssignments_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAssignments_User_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAssignments_WorkoutTemplate_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplate",
                        principalColumn: "WorkoutTemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rep",
                columns: table => new
                {
                    RepId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    weight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    repnumber = table.Column<int>(type: "int", nullable: false),
                    success = table.Column<bool>(type: "bit", nullable: false),
                    SetsSetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rep", x => x.RepId);
                    table.ForeignKey(
                        name: "FK_Rep_Set_SetsSetId",
                        column: x => x.SetsSetId,
                        principalTable: "Set",
                        principalColumn: "SetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseFeedbacks",
                columns: table => new
                {
                    ExerciseFeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutFeedbackId = table.Column<int>(type: "int", nullable: false),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    RPE = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    TooHeavy = table.Column<bool>(type: "bit", nullable: false),
                    TooLight = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseFeedbacks", x => x.ExerciseFeedbackId);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_Set_SetId",
                        column: x => x.SetId,
                        principalTable: "Set",
                        principalColumn: "SetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_WorkoutFeedbacks_WorkoutFeedbackId",
                        column: x => x.WorkoutFeedbackId,
                        principalTable: "WorkoutFeedbacks",
                        principalColumn: "WorkoutFeedbackId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    WorkoutSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    TemplatesUsed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: true),
                    TemplateAssignmentId = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsFromCoach = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    IterationNumber = table.Column<int>(type: "int", nullable: false),
                    PreviousIterationId = table.Column<int>(type: "int", nullable: true),
                    NextIterationId = table.Column<int>(type: "int", nullable: true),
                    WorkoutFeedbackId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.WorkoutSessionId);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutFeedbacks_WorkoutFeedbackId",
                        column: x => x.WorkoutFeedbackId,
                        principalTable: "WorkoutFeedbacks",
                        principalColumn: "WorkoutFeedbackId");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutSessions_NextIterationId",
                        column: x => x.NextIterationId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "WorkoutSessionId");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutSessions_PreviousIterationId",
                        column: x => x.PreviousIterationId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "WorkoutSessionId");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutTemplate_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplate",
                        principalColumn: "WorkoutTemplateId");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateSet",
                columns: table => new
                {
                    WorkoutTemplateSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateExerciseId = table.Column<int>(type: "int", nullable: false),
                    SettypeId = table.Column<int>(type: "int", nullable: false),
                    DefaultReps = table.Column<int>(type: "int", nullable: false),
                    DefaultWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateSet", x => x.WorkoutTemplateSetId);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateSet_Settype_SettypeId",
                        column: x => x.SettypeId,
                        principalTable: "Settype",
                        principalColumn: "SettypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateSet_WorkoutTemplateExercise_WorkoutTemplateExerciseId",
                        column: x => x.WorkoutTemplateExerciseId,
                        principalTable: "WorkoutTemplateExercise",
                        principalColumn: "WorkoutTemplateExerciseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalId = table.Column<int>(type: "int", nullable: false),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FeedbackType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalFeedback_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoalFeedback_ClientGoals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "ClientGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAutomaticUpdate = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalMilestones_ClientGoals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "ClientGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSchedules",
                columns: table => new
                {
                    WorkoutScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateAssignmentId = table.Column<int>(type: "int", nullable: true),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecurrenceDayOfWeek = table.Column<int>(type: "int", nullable: true),
                    MultipleDaysOfWeek = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecurrenceDayOfMonth = table.Column<int>(type: "int", nullable: true),
                    SendReminder = table.Column<bool>(type: "bit", nullable: false),
                    ReminderHoursBefore = table.Column<int>(type: "int", nullable: false),
                    LastReminderSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastGeneratedWorkoutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastGeneratedSessionId = table.Column<int>(type: "int", nullable: true),
                    TotalWorkoutsGenerated = table.Column<int>(type: "int", nullable: false),
                    LastGenerationStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateAssignmentId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSchedules", x => x.WorkoutScheduleId);
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_Session_LastGeneratedSessionId",
                        column: x => x.LastGeneratedSessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_TemplateAssignments_TemplateAssignmentId",
                        column: x => x.TemplateAssignmentId,
                        principalTable: "TemplateAssignments",
                        principalColumn: "TemplateAssignmentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_TemplateAssignments_TemplateAssignmentId1",
                        column: x => x.TemplateAssignmentId1,
                        principalTable: "TemplateAssignments",
                        principalColumn: "TemplateAssignmentId");
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_User_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_WorkoutTemplate_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "WorkoutTemplate",
                        principalColumn: "WorkoutTemplateId");
                });

            migrationBuilder.CreateTable(
                name: "ShareToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    MaxAccessCount = table.Column<int>(type: "int", nullable: true),
                    AllowSessionAccess = table.Column<bool>(type: "bit", nullable: false),
                    AllowReportAccess = table.Column<bool>(type: "bit", nullable: false),
                    AllowCalculatorAccess = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    WorkoutSessionId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareToken_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShareToken_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareToken_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "WorkoutSessionId");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExercises",
                columns: table => new
                {
                    WorkoutExerciseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutSessionId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: true),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestPeriodSeconds = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExercises", x => x.WorkoutExerciseId);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentId");
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "WorkoutSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressionRules",
                columns: table => new
                {
                    ProgressionRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateExerciseId = table.Column<int>(type: "int", nullable: true),
                    WorkoutTemplateSetId = table.Column<int>(type: "int", nullable: true),
                    ClientUserId = table.Column<int>(type: "int", nullable: true),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IncrementValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ConsecutiveSuccessesRequired = table.Column<int>(type: "int", nullable: false),
                    SuccessThreshold = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaximumValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ApplyDeload = table.Column<bool>(type: "bit", nullable: false),
                    DeloadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ConsecutiveFailuresForDeload = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressionRules", x => x.ProgressionRuleId);
                    table.ForeignKey(
                        name: "FK_ProgressionRules_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ProgressionRules_User_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgressionRules_WorkoutTemplateExercise_WorkoutTemplateExerciseId",
                        column: x => x.WorkoutTemplateExerciseId,
                        principalTable: "WorkoutTemplateExercise",
                        principalColumn: "WorkoutTemplateExerciseId");
                    table.ForeignKey(
                        name: "FK_ProgressionRules_WorkoutTemplateSet_WorkoutTemplateSetId",
                        column: x => x.WorkoutTemplateSetId,
                        principalTable: "WorkoutTemplateSet",
                        principalColumn: "WorkoutTemplateSetId");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSets",
                columns: table => new
                {
                    WorkoutSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutExerciseId = table.Column<int>(type: "int", nullable: false),
                    SettypeId = table.Column<int>(type: "int", nullable: true),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    SetNumber = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: true),
                    TargetMinReps = table.Column<int>(type: "int", nullable: true),
                    TargetMaxReps = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    Distance = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Intensity = table.Column<int>(type: "int", nullable: true),
                    RPE = table.Column<int>(type: "int", nullable: true),
                    RestSeconds = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSets", x => x.WorkoutSetId);
                    table.ForeignKey(
                        name: "FK_WorkoutSets_Settype_SettypeId",
                        column: x => x.SettypeId,
                        principalTable: "Settype",
                        principalColumn: "SettypeId");
                    table.ForeignKey(
                        name: "FK_WorkoutSets_WorkoutExercises_WorkoutExerciseId",
                        column: x => x.WorkoutExerciseId,
                        principalTable: "WorkoutExercises",
                        principalColumn: "WorkoutExerciseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressionHistories",
                columns: table => new
                {
                    ProgressionHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgressionRuleId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NewValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppliedAutomatically = table.Column<bool>(type: "bit", nullable: false),
                    CoachOverride = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressionHistories", x => x.ProgressionHistoryId);
                    table.ForeignKey(
                        name: "FK_ProgressionHistories_ProgressionRules_ProgressionRuleId",
                        column: x => x.ProgressionRuleId,
                        principalTable: "ProgressionRules",
                        principalColumn: "ProgressionRuleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgressionHistories_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alert_AlertThresholdId",
                table: "Alert",
                column: "AlertThresholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_TriggeredAt",
                table: "Alert",
                column: "TriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistory_TriggeredAt",
                table: "AlertHistory",
                column: "TriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertThreshold_MetricName",
                table: "AlertThreshold",
                column: "MetricName");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_ActivityDate",
                table: "ClientActivities",
                column: "ActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_ClientId",
                table: "ClientActivities",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_CoachId",
                table: "ClientActivities",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_IsViewedByCoach",
                table: "ClientActivities",
                column: "IsViewedByCoach");

            migrationBuilder.CreateIndex(
                name: "IX_ClientEquipments_ClientUserId",
                table: "ClientEquipments",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientEquipments_IsAvailable",
                table: "ClientEquipments",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_ClientUserId",
                table: "ClientExerciseExclusions",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_CreatedByCoachId",
                table: "ClientExerciseExclusions",
                column: "CreatedByCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_ExerciseTypeId",
                table: "ClientExerciseExclusions",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_IsActive",
                table: "ClientExerciseExclusions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGoals_CoachClientRelationshipId",
                table: "ClientGoals",
                column: "CoachClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGoals_IsActive",
                table: "ClientGoals",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGroupMembers_ClientGroupId",
                table: "ClientGroupMembers",
                column: "ClientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGroupMembers_CoachClientRelationshipId",
                table: "ClientGroupMembers",
                column: "CoachClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGroups_CoachId",
                table: "ClientGroups",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientMessages_CoachClientRelationshipId",
                table: "CoachClientMessages",
                column: "CoachClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientPermissions_CoachClientRelationshipId",
                table: "CoachClientPermissions",
                column: "CoachClientRelationshipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_AppUserId",
                table: "CoachClientRelationships",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_AppUserId1",
                table: "CoachClientRelationships",
                column: "AppUserId1");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_ClientGroupId",
                table: "CoachClientRelationships",
                column: "ClientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_ClientId",
                table: "CoachClientRelationships",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachId",
                table: "CoachClientRelationships",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachId_ClientId",
                table: "CoachClientRelationships",
                columns: new[] { "CoachId", "ClientId" },
                unique: true,
                filter: "[ClientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_Status",
                table: "CoachClientRelationships",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_CoachClientRelationshipId",
                table: "CoachNotes",
                column: "CoachClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_IsVisibleToClient",
                table: "CoachNotes",
                column: "IsVisibleToClient");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_SetId",
                table: "ExerciseFeedbacks",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_WorkoutFeedbackId",
                table: "ExerciseFeedbacks",
                column: "WorkoutFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_CreatedByCoachId",
                table: "ExerciseSubstitutions",
                column: "CreatedByCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_IsGlobal",
                table: "ExerciseSubstitutions",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_MovementPattern",
                table: "ExerciseSubstitutions",
                column: "MovementPattern");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_PrimaryExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "PrimaryExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_SubstituteExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "SubstituteExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_UserId",
                table: "Feedback",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTermRelatedTerms_RelatedTermsId",
                table: "GlossaryTermRelatedTerms",
                column: "RelatedTermsId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalFeedback_CoachId",
                table: "GoalFeedback",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalFeedback_GoalId",
                table: "GoalFeedback",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalFeedback_IsRead",
                table: "GoalFeedback",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_GoalMilestones_Date",
                table: "GoalMilestones",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_GoalMilestones_GoalId",
                table: "GoalMilestones",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpArticle_HelpCategoryId",
                table: "HelpArticle",
                column: "HelpCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpArticleRelatedArticles_RelatedArticlesId",
                table: "HelpArticleRelatedArticles",
                column: "RelatedArticlesId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpCategory_ParentCategoryId",
                table: "HelpCategory",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_AlertId",
                table: "Notification",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId_IsRead",
                table: "Notification",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionHistories_ApplicationDate",
                table: "ProgressionHistories",
                column: "ApplicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionHistories_ProgressionRuleId",
                table: "ProgressionHistories",
                column: "ProgressionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionHistories_SessionId",
                table: "ProgressionHistories",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_ClientUserId",
                table: "ProgressionRules",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_CoachUserId",
                table: "ProgressionRules",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_IsActive",
                table: "ProgressionRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_WorkoutTemplateExerciseId",
                table: "ProgressionRules",
                column: "WorkoutTemplateExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_WorkoutTemplateSetId",
                table: "ProgressionRules",
                column: "WorkoutTemplateSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Rep_SetsSetId",
                table: "Rep",
                column: "SetsSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Session_UserId",
                table: "Session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Set_ExerciseTypeId",
                table: "Set",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Set_SessionId",
                table: "Set",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Set_SettypeId",
                table: "Set",
                column: "SettypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareToken_SessionId",
                table: "ShareToken",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareToken_UserId",
                table: "ShareToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareToken_WorkoutSessionId",
                table: "ShareToken",
                column: "WorkoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_ClientRelationshipId",
                table: "TemplateAssignments",
                column: "ClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_ClientUserId",
                table: "TemplateAssignments",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_CoachUserId",
                table: "TemplateAssignments",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_IsActive",
                table: "TemplateAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_WorkoutTemplateId",
                table: "TemplateAssignments",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_EquipmentId",
                table: "WorkoutExercises",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseTypeId",
                table: "WorkoutExercises",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_WorkoutSessionId",
                table: "WorkoutExercises",
                column: "WorkoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_ClientUserId",
                table: "WorkoutFeedbacks",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_CoachNotified",
                table: "WorkoutFeedbacks",
                column: "CoachNotified");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_CoachViewed",
                table: "WorkoutFeedbacks",
                column: "CoachViewed");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_SessionId",
                table: "WorkoutFeedbacks",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_ClientUserId",
                table: "WorkoutSchedules",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_CoachUserId",
                table: "WorkoutSchedules",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_IsActive",
                table: "WorkoutSchedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_TemplateAssignmentId",
                table: "WorkoutSchedules",
                column: "TemplateAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_TemplateAssignmentId1",
                table: "WorkoutSchedules",
                column: "TemplateAssignmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_TemplateId",
                table: "WorkoutSchedules",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_NextIterationId",
                table: "WorkoutSessions",
                column: "NextIterationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_PreviousIterationId",
                table: "WorkoutSessions",
                column: "PreviousIterationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_SessionId",
                table: "WorkoutSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutFeedbackId",
                table: "WorkoutSessions",
                column: "WorkoutFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutTemplateId",
                table: "WorkoutSessions",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_SettypeId",
                table: "WorkoutSets",
                column: "SettypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutExerciseId",
                table: "WorkoutSets",
                column: "WorkoutExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplate_Category",
                table: "WorkoutTemplate",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplate_UserId",
                table: "WorkoutTemplate",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_EquipmentId",
                table: "WorkoutTemplateExercise",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_ExerciseTypeId",
                table: "WorkoutTemplateExercise",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_SequenceNum",
                table: "WorkoutTemplateExercise",
                column: "SequenceNum");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_WorkoutTemplateId",
                table: "WorkoutTemplateExercise",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateSet_SequenceNum",
                table: "WorkoutTemplateSet",
                column: "SequenceNum");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateSet_SettypeId",
                table: "WorkoutTemplateSet",
                column: "SettypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateSet_WorkoutTemplateExerciseId",
                table: "WorkoutTemplateSet",
                column: "WorkoutTemplateExerciseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertHistory");

            migrationBuilder.DropTable(
                name: "ClientActivities");

            migrationBuilder.DropTable(
                name: "ClientEquipments");

            migrationBuilder.DropTable(
                name: "ClientExerciseExclusions");

            migrationBuilder.DropTable(
                name: "ClientGroupMembers");

            migrationBuilder.DropTable(
                name: "CoachClientMessages");

            migrationBuilder.DropTable(
                name: "CoachClientPermissions");

            migrationBuilder.DropTable(
                name: "CoachNotes");

            migrationBuilder.DropTable(
                name: "ExerciseFeedbacks");

            migrationBuilder.DropTable(
                name: "ExerciseSubstitutions");

            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "GlossaryTermRelatedTerms");

            migrationBuilder.DropTable(
                name: "GoalFeedback");

            migrationBuilder.DropTable(
                name: "GoalMilestones");

            migrationBuilder.DropTable(
                name: "HelpArticleRelatedArticles");

            migrationBuilder.DropTable(
                name: "LoginHistory");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PendingExerciseSelection");

            migrationBuilder.DropTable(
                name: "ProgressionHistories");

            migrationBuilder.DropTable(
                name: "Rep");

            migrationBuilder.DropTable(
                name: "ShareToken");

            migrationBuilder.DropTable(
                name: "WorkoutSchedules");

            migrationBuilder.DropTable(
                name: "WorkoutSets");

            migrationBuilder.DropTable(
                name: "GlossaryTerm");

            migrationBuilder.DropTable(
                name: "ClientGoals");

            migrationBuilder.DropTable(
                name: "HelpArticle");

            migrationBuilder.DropTable(
                name: "Alert");

            migrationBuilder.DropTable(
                name: "ProgressionRules");

            migrationBuilder.DropTable(
                name: "Set");

            migrationBuilder.DropTable(
                name: "TemplateAssignments");

            migrationBuilder.DropTable(
                name: "WorkoutExercises");

            migrationBuilder.DropTable(
                name: "HelpCategory");

            migrationBuilder.DropTable(
                name: "AlertThreshold");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateSet");

            migrationBuilder.DropTable(
                name: "CoachClientRelationships");

            migrationBuilder.DropTable(
                name: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "Settype");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateExercise");

            migrationBuilder.DropTable(
                name: "ClientGroups");

            migrationBuilder.DropTable(
                name: "WorkoutFeedbacks");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "ExerciseType");

            migrationBuilder.DropTable(
                name: "WorkoutTemplate");

            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "Session");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
