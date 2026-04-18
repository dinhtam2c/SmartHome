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
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
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
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalTargets = table.Column<int>(type: "INTEGER", nullable: false),
                    PendingTargets = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedTargets = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulTargets = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedTargets = table.Column<int>(type: "INTEGER", nullable: false)
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
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
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
                name: "SceneExecutionSideEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneSideEffectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParamsPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Timing = table.Column<int>(type: "INTEGER", nullable: false),
                    DelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CommandCorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneExecutionSideEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneExecutionSideEffects_SceneExecutions_SceneExecutionId",
                        column: x => x.SceneExecutionId,
                        principalTable: "SceneExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SceneExecutionTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneTargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DesiredStatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CommandCorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UnresolvedDiffPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneExecutionTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneExecutionTargets_SceneExecutions_SceneExecutionId",
                        column: x => x.SceneExecutionId,
                        principalTable: "SceneExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SceneSideEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParamsPayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Timing = table.Column<int>(type: "INTEGER", nullable: false),
                    DelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneSideEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneSideEffects_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SceneTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SceneId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CapabilityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DesiredStatePayload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneTargets_Scenes_SceneId",
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
                name: "IX_Rooms_HomeId",
                table: "Rooms",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutions_SceneId_StartedAt",
                table: "SceneExecutions",
                columns: new[] { "SceneId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutionSideEffects_DeviceId_CommandCorrelationId",
                table: "SceneExecutionSideEffects",
                columns: new[] { "DeviceId", "CommandCorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutionSideEffects_SceneExecutionId_Order",
                table: "SceneExecutionSideEffects",
                columns: new[] { "SceneExecutionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutionTargets_DeviceId_CommandCorrelationId",
                table: "SceneExecutionTargets",
                columns: new[] { "DeviceId", "CommandCorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneExecutionTargets_SceneExecutionId_Order",
                table: "SceneExecutionTargets",
                columns: new[] { "SceneExecutionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_HomeId",
                table: "Scenes",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_HomeId_Name",
                table: "Scenes",
                columns: new[] { "HomeId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneSideEffects_DeviceId_CapabilityId_EndpointId",
                table: "SceneSideEffects",
                columns: new[] { "DeviceId", "CapabilityId", "EndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneSideEffects_SceneId_Order",
                table: "SceneSideEffects",
                columns: new[] { "SceneId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneTargets_DeviceId_CapabilityId_EndpointId",
                table: "SceneTargets",
                columns: new[] { "DeviceId", "CapabilityId", "EndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneTargets_SceneId_Order",
                table: "SceneTargets",
                columns: new[] { "SceneId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceCapabilities");

            migrationBuilder.DropTable(
                name: "DeviceCapabilityStateHistories");

            migrationBuilder.DropTable(
                name: "DeviceCommandExecutions");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "SceneExecutionSideEffects");

            migrationBuilder.DropTable(
                name: "SceneExecutionTargets");

            migrationBuilder.DropTable(
                name: "SceneSideEffects");

            migrationBuilder.DropTable(
                name: "SceneTargets");

            migrationBuilder.DropTable(
                name: "DeviceEndpoints");

            migrationBuilder.DropTable(
                name: "Homes");

            migrationBuilder.DropTable(
                name: "SceneExecutions");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
