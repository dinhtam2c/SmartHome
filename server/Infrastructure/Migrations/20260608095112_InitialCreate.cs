using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TriggerDeviceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TriggerEndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TriggerCapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TriggerStatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TriggerSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ContinueOnError = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailureBranchSelected = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalActions = table.Column<int>(type: "INTEGER", nullable: false),
                    PendingActions = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedActions = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulActions = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedActions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConditionLogic = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CooldownMs = table.Column<int>(type: "INTEGER", nullable: false),
                    LastEvaluationResult = table.Column<bool>(type: "INTEGER", nullable: true),
                    LastEvaluatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastTriggeredAt = table.Column<long>(type: "INTEGER", nullable: true),
                    TimeWindowEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeWindowStartMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    TimeWindowEndMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    TimeWindowDaysOfWeekMask = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ContinueOnError = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MacAddress = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Protocol = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ProvisionState = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ProvisionCode = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    AccessToken = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ProvisionedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Uptime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Homes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Homes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SceneExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TriggerSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ContinueOnError = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailureBranchSelected = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalActions = table.Column<int>(type: "INTEGER", nullable: false),
                    PendingActions = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedActions = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulActions = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedActions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ContinueOnError = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationExecutionActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AutomationExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AutomationActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    OptionsPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CommandCorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UnresolvedDiffPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationExecutionActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationExecutionActions_AutomationExecutions_AutomationExecutionId",
                        column: x => x.AutomationExecutionId,
                        principalTable: "AutomationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    OptionsPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationActions_AutomationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "AutomationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomationConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FieldPath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Operator = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CompareValuePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationConditions_AutomationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "AutomationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCapabilityStateHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ReportedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCapabilityStateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCapabilityStateHistories_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommandExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ResultPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RequestedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommandExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCommandExecutions_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEndpoints_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CanvasWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    CanvasHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Floors_Homes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "Homes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Homes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "Homes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SceneExecutionActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    OptionsPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CommandCorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UnresolvedDiffPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneExecutionActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneExecutionActions_SceneExecutions_SceneExecutionId",
                        column: x => x.SceneExecutionId,
                        principalTable: "SceneExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SceneActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    OptionsPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneActions_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    SupportedOperations = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    LastReportedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCapabilities_DeviceEndpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "DeviceEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FloorPlacedDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FloorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FloorRoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlacedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlacedDevices_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FloorRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FloorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LinkedRoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Polygon = table.Column<string>(type: "TEXT", nullable: false),
                    FillColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorRooms_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationActions_DeviceId_CapabilityId_EndpointId",
                table: "AutomationActions",
                columns: new[] { "DeviceId", "CapabilityId", "EndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationActions_RuleId_Section_Order",
                table: "AutomationActions",
                columns: new[] { "RuleId", "Section", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConditions_DeviceId_EndpointId_CapabilityId",
                table: "AutomationConditions",
                columns: new[] { "DeviceId", "EndpointId", "CapabilityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConditions_RuleId_Order",
                table: "AutomationConditions",
                columns: new[] { "RuleId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecutionActions_AutomationExecutionId_Section_Order",
                table: "AutomationExecutionActions",
                columns: new[] { "AutomationExecutionId", "Section", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecutionActions_DeviceId_CommandCorrelationId",
                table: "AutomationExecutionActions",
                columns: new[] { "DeviceId", "CommandCorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecutions_HomeId",
                table: "AutomationExecutions",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecutions_RuleId_StartedAt",
                table: "AutomationExecutions",
                columns: new[] { "RuleId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_HomeId",
                table: "AutomationRules",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_HomeId_Name",
                table: "AutomationRules",
                columns: new[] { "HomeId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCapabilities_EndpointId_CapabilityId",
                table: "DeviceCapabilities",
                columns: new[] { "EndpointId", "CapabilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCapabilityStateHistories_DeviceId_CapabilityId_EndpointId_ReportedAt",
                table: "DeviceCapabilityStateHistories",
                columns: new[] { "DeviceId", "CapabilityId", "EndpointId", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommandExecutions_DeviceId_CorrelationId",
                table: "DeviceCommandExecutions",
                columns: new[] { "DeviceId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommandExecutions_DeviceId_RequestedAt",
                table: "DeviceCommandExecutions",
                columns: new[] { "DeviceId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEndpoints_DeviceId_EndpointId",
                table: "DeviceEndpoints",
                columns: new[] { "DeviceId", "EndpointId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_AccessToken",
                table: "Devices",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_HomeId",
                table: "Devices",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_MacAddress",
                table: "Devices",
                column: "MacAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ProvisionCode",
                table: "Devices",
                column: "ProvisionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_RoomId",
                table: "Devices",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlacedDevices_DeviceId",
                table: "FloorPlacedDevices",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlacedDevices_FloorId",
                table: "FloorPlacedDevices",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorRooms_FloorId",
                table: "FloorRooms",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorRooms_LinkedRoomId",
                table: "FloorRooms",
                column: "LinkedRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_HomeId",
                table: "Floors",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_HomeId_SortOrder",
                table: "Floors",
                columns: new[] { "HomeId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HomeId",
                table: "Rooms",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneActions_DeviceId_CapabilityId_EndpointId",
                table: "SceneActions",
                columns: new[] { "DeviceId", "CapabilityId", "EndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneActions_SceneId_Section_Order",
                table: "SceneActions",
                columns: new[] { "SceneId", "Section", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutionActions_DeviceId_CommandCorrelationId",
                table: "SceneExecutionActions",
                columns: new[] { "DeviceId", "CommandCorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutionActions_SceneExecutionId_Section_Order",
                table: "SceneExecutionActions",
                columns: new[] { "SceneExecutionId", "Section", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutions_HomeId",
                table: "SceneExecutions",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutions_SceneId_StartedAt",
                table: "SceneExecutions",
                columns: new[] { "SceneId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_HomeId",
                table: "Scenes",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_HomeId_Name",
                table: "Scenes",
                columns: new[] { "HomeId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationActions");

            migrationBuilder.DropTable(
                name: "AutomationConditions");

            migrationBuilder.DropTable(
                name: "AutomationExecutionActions");

            migrationBuilder.DropTable(
                name: "DeviceCapabilities");

            migrationBuilder.DropTable(
                name: "DeviceCapabilityStateHistories");

            migrationBuilder.DropTable(
                name: "DeviceCommandExecutions");

            migrationBuilder.DropTable(
                name: "FloorPlacedDevices");

            migrationBuilder.DropTable(
                name: "FloorRooms");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "SceneActions");

            migrationBuilder.DropTable(
                name: "SceneExecutionActions");

            migrationBuilder.DropTable(
                name: "AutomationRules");

            migrationBuilder.DropTable(
                name: "AutomationExecutions");

            migrationBuilder.DropTable(
                name: "DeviceEndpoints");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "SceneExecutions");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Homes");
        }
    }
}
