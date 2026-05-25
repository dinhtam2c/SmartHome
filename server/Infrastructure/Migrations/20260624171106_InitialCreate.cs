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
                name: "ActionSetExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionSetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ContinueOnError = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSetExecutions", x => x.Id);
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
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRules", x => x.Id);
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
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
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
                    CompareValuePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
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
                name: "ActionSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ContinueOnError = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActionSetType = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SceneId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionSets_AutomationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "AutomationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionSets_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_Devices_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FloorPlanRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FloorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Polygon = table.Column<string>(type: "TEXT", nullable: false),
                    FillColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlanRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlanRooms_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloorPlanRooms_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionSetActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionSetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSetActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionSetActions_ActionSets_ActionSetId",
                        column: x => x.ActionSetId,
                        principalTable: "ActionSets",
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
                name: "FloorDevicePlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FloorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorDevicePlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorDevicePlacements_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FloorDevicePlacements_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionSetActionExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceCommandExecutionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSetActionExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionSetActionExecutions_ActionSetExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "ActionSetExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionSetActionExecutions_DeviceCommandExecutions_DeviceCommandExecutionId",
                        column: x => x.DeviceCommandExecutionId,
                        principalTable: "DeviceCommandExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateIndex(
                name: "IX_ActionSetActionExecutions_DeviceCommandExecutionId",
                table: "ActionSetActionExecutions",
                column: "DeviceCommandExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionSetActionExecutions_ExecutionId_Section_Order",
                table: "ActionSetActionExecutions",
                columns: new[] { "ExecutionId", "Section", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionSetActions_ActionSetId_Section_Order",
                table: "ActionSetActions",
                columns: new[] { "ActionSetId", "Section", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionSetActions_DeviceId_CapabilityId_EndpointId",
                table: "ActionSetActions",
                columns: new[] { "DeviceId", "CapabilityId", "EndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionSetExecutions_ActionSetId",
                table: "ActionSetExecutions",
                column: "ActionSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSetExecutions_HomeId",
                table: "ActionSetExecutions",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSetExecutions_SourceType_SourceId_StartedAt",
                table: "ActionSetExecutions",
                columns: new[] { "SourceType", "SourceId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionSets_RuleId",
                table: "ActionSets",
                column: "RuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionSets_SceneId",
                table: "ActionSets",
                column: "SceneId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConditions_DeviceId_EndpointId_CapabilityId",
                table: "AutomationConditions",
                columns: new[] { "DeviceId", "EndpointId", "CapabilityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConditions_RuleId_Order",
                table: "AutomationConditions",
                columns: new[] { "RuleId", "Order" });

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
                name: "IX_FloorDevicePlacements_DeviceId",
                table: "FloorDevicePlacements",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorDevicePlacements_FloorId",
                table: "FloorDevicePlacements",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanRooms_FloorId",
                table: "FloorPlanRooms",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanRooms_RoomId",
                table: "FloorPlanRooms",
                column: "RoomId",
                unique: true);

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
                name: "ActionSetActionExecutions");

            migrationBuilder.DropTable(
                name: "ActionSetActions");

            migrationBuilder.DropTable(
                name: "AutomationConditions");

            migrationBuilder.DropTable(
                name: "DeviceCapabilities");

            migrationBuilder.DropTable(
                name: "FloorDevicePlacements");

            migrationBuilder.DropTable(
                name: "FloorPlanRooms");

            migrationBuilder.DropTable(
                name: "ActionSetExecutions");

            migrationBuilder.DropTable(
                name: "DeviceCommandExecutions");

            migrationBuilder.DropTable(
                name: "ActionSets");

            migrationBuilder.DropTable(
                name: "DeviceEndpoints");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropTable(
                name: "AutomationRules");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Homes");
        }
    }
}
