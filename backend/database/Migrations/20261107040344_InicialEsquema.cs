using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class InicialEsquema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medico",
                columns: table => new
                {
                    id_medico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    apellido = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    colegiado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    especialidad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medico", x => x.id_medico);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "paciente",
                columns: table => new
                {
                    id_paciente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    apellido = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cui = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    nit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    telefono = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    correo_electronico = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    genero = table.Column<string>(type: "char(1)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo_sangre = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paciente", x => x.id_paciente);
                    table.CheckConstraint("ck_paciente_genero", "`genero` IN ('F','M')");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "proveedor",
                columns: table => new
                {
                    id_proveedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    telefono = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    correo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedor", x => x.id_proveedor);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id_rol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.id_rol);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tipo_cobertura",
                columns: table => new
                {
                    id_tipo_cobertura = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    porcentaje_cobertura = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_cobertura", x => x.id_tipo_cobertura);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cita",
                columns: table => new
                {
                    id_cita = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_medico = table.Column<int>(type: "int", nullable: false),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    fecha_cita = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    motivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cita", x => x.id_cita);
                    table.ForeignKey(
                        name: "FK_cita_medico_id_medico",
                        column: x => x.id_medico,
                        principalTable: "medico",
                        principalColumn: "id_medico",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cita_paciente_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "paciente",
                        principalColumn: "id_paciente",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "historial_clinico",
                columns: table => new
                {
                    id_historial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    fecha_apertura = table.Column<DateOnly>(type: "date", nullable: false),
                    antecedentes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_clinico", x => x.id_historial);
                    table.ForeignKey(
                        name: "FK_historial_clinico_paciente_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "paciente",
                        principalColumn: "id_paciente",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "insumo",
                columns: table => new
                {
                    id_insumo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_proveedor = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    precio = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    stock_minimo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insumo", x => x.id_insumo);
                    table.ForeignKey(
                        name: "FK_insumo_proveedor_id_proveedor",
                        column: x => x.id_proveedor,
                        principalTable: "proveedor",
                        principalColumn: "id_proveedor",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    correo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_rol = table.Column<int>(type: "int", nullable: false),
                    id_medico = table.Column<int>(type: "int", nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id_usuario);
                    table.ForeignKey(
                        name: "FK_usuario_medico_id_medico",
                        column: x => x.id_medico,
                        principalTable: "medico",
                        principalColumn: "id_medico",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_usuario_rol_id_rol",
                        column: x => x.id_rol,
                        principalTable: "rol",
                        principalColumn: "id_rol",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "asociado_seguro",
                columns: table => new
                {
                    id_asegurado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    id_tipo_cobertura = table.Column<int>(type: "int", nullable: false),
                    aseguradora = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    numero_poliza = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vigencia_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    vigencia_hasta = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asociado_seguro", x => x.id_asegurado);
                    table.ForeignKey(
                        name: "FK_asociado_seguro_paciente_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "paciente",
                        principalColumn: "id_paciente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asociado_seguro_tipo_cobertura_id_tipo_cobertura",
                        column: x => x.id_tipo_cobertura,
                        principalTable: "tipo_cobertura",
                        principalColumn: "id_tipo_cobertura",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "estudio",
                columns: table => new
                {
                    id_estudio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_cita = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_solicitud = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estudio", x => x.id_estudio);
                    table.ForeignKey(
                        name: "FK_estudio_cita_id_cita",
                        column: x => x.id_cita,
                        principalTable: "cita",
                        principalColumn: "id_cita",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "diagnostico",
                columns: table => new
                {
                    id_diagnosticos = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_historial = table.Column<int>(type: "int", nullable: false),
                    id_cita = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    codigo_cie10 = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alergias = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diagnostico", x => x.id_diagnosticos);
                    table.ForeignKey(
                        name: "FK_diagnostico_cita_id_cita",
                        column: x => x.id_cita,
                        principalTable: "cita",
                        principalColumn: "id_cita",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_diagnostico_historial_clinico_id_historial",
                        column: x => x.id_historial,
                        principalTable: "historial_clinico",
                        principalColumn: "id_historial",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kardex",
                columns: table => new
                {
                    id_kardex = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_insumo = table.Column<int>(type: "int", nullable: false),
                    id_proveedor = table.Column<int>(type: "int", nullable: true),
                    tipo_movimiento = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    referencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kardex", x => x.id_kardex);
                    table.ForeignKey(
                        name: "FK_kardex_insumo_id_insumo",
                        column: x => x.id_insumo,
                        principalTable: "insumo",
                        principalColumn: "id_insumo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_kardex_proveedor_id_proveedor",
                        column: x => x.id_proveedor,
                        principalTable: "proveedor",
                        principalColumn: "id_proveedor",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "facturacion",
                columns: table => new
                {
                    id_factura = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    id_cita = table.Column<int>(type: "int", nullable: false),
                    id_asegurado = table.Column<int>(type: "int", nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    monto_cobertura = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturacion", x => x.id_factura);
                    table.ForeignKey(
                        name: "FK_facturacion_asociado_seguro_id_asegurado",
                        column: x => x.id_asegurado,
                        principalTable: "asociado_seguro",
                        principalColumn: "id_asegurado",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_facturacion_cita_id_cita",
                        column: x => x.id_cita,
                        principalTable: "cita",
                        principalColumn: "id_cita",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facturacion_paciente_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "paciente",
                        principalColumn: "id_paciente",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "receta",
                columns: table => new
                {
                    id_receta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_cita = table.Column<int>(type: "int", nullable: false),
                    id_diagnostico = table.Column<int>(type: "int", nullable: true),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    indicaciones = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receta", x => x.id_receta);
                    table.ForeignKey(
                        name: "FK_receta_cita_id_cita",
                        column: x => x.id_cita,
                        principalTable: "cita",
                        principalColumn: "id_cita",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receta_diagnostico_id_diagnostico",
                        column: x => x.id_diagnostico,
                        principalTable: "diagnostico",
                        principalColumn: "id_diagnosticos",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "resultado_tratante",
                columns: table => new
                {
                    id_resultado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_estudio = table.Column<int>(type: "int", nullable: true),
                    id_medico = table.Column<int>(type: "int", nullable: false),
                    id_diagnostico = table.Column<int>(type: "int", nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conclusion = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resultado_tratante", x => x.id_resultado);
                    table.ForeignKey(
                        name: "FK_resultado_tratante_diagnostico_id_diagnostico",
                        column: x => x.id_diagnostico,
                        principalTable: "diagnostico",
                        principalColumn: "id_diagnosticos",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resultado_tratante_estudio_id_estudio",
                        column: x => x.id_estudio,
                        principalTable: "estudio",
                        principalColumn: "id_estudio",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resultado_tratante_medico_id_medico",
                        column: x => x.id_medico,
                        principalTable: "medico",
                        principalColumn: "id_medico",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "facturacion_detalle",
                columns: table => new
                {
                    id_detalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_factura = table.Column<int>(type: "int", nullable: false),
                    concepto = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facturacion_detalle", x => x.id_detalle);
                    table.ForeignKey(
                        name: "FK_facturacion_detalle_facturacion_id_factura",
                        column: x => x.id_factura,
                        principalTable: "facturacion",
                        principalColumn: "id_factura",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "receta_detalle",
                columns: table => new
                {
                    id_receta_detalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_receta = table.Column<int>(type: "int", nullable: false),
                    id_insumo = table.Column<int>(type: "int", nullable: false),
                    dosis = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    frecuencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    duracion_dias = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receta_detalle", x => x.id_receta_detalle);
                    table.ForeignKey(
                        name: "FK_receta_detalle_insumo_id_insumo",
                        column: x => x.id_insumo,
                        principalTable: "insumo",
                        principalColumn: "id_insumo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receta_detalle_receta_id_receta",
                        column: x => x.id_receta,
                        principalTable: "receta",
                        principalColumn: "id_receta",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_asociado_seguro_id_paciente",
                table: "asociado_seguro",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_asociado_seguro_id_tipo_cobertura",
                table: "asociado_seguro",
                column: "id_tipo_cobertura");

            migrationBuilder.CreateIndex(
                name: "IX_cita_id_medico",
                table: "cita",
                column: "id_medico");

            migrationBuilder.CreateIndex(
                name: "IX_cita_id_paciente",
                table: "cita",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_diagnostico_id_cita",
                table: "diagnostico",
                column: "id_cita");

            migrationBuilder.CreateIndex(
                name: "IX_diagnostico_id_historial",
                table: "diagnostico",
                column: "id_historial");

            migrationBuilder.CreateIndex(
                name: "IX_estudio_id_cita",
                table: "estudio",
                column: "id_cita");

            migrationBuilder.CreateIndex(
                name: "IX_facturacion_id_asegurado",
                table: "facturacion",
                column: "id_asegurado");

            migrationBuilder.CreateIndex(
                name: "IX_facturacion_id_cita",
                table: "facturacion",
                column: "id_cita",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facturacion_id_paciente",
                table: "facturacion",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_facturacion_detalle_id_factura",
                table: "facturacion_detalle",
                column: "id_factura");

            migrationBuilder.CreateIndex(
                name: "IX_historial_clinico_id_paciente",
                table: "historial_clinico",
                column: "id_paciente",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_insumo_id_proveedor",
                table: "insumo",
                column: "id_proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_id_insumo",
                table: "kardex",
                column: "id_insumo");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_id_proveedor",
                table: "kardex",
                column: "id_proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_medico_colegiado",
                table: "medico",
                column: "colegiado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paciente_cui",
                table: "paciente",
                column: "cui",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receta_id_cita",
                table: "receta",
                column: "id_cita");

            migrationBuilder.CreateIndex(
                name: "IX_receta_id_diagnostico",
                table: "receta",
                column: "id_diagnostico");

            migrationBuilder.CreateIndex(
                name: "IX_receta_detalle_id_insumo",
                table: "receta_detalle",
                column: "id_insumo");

            migrationBuilder.CreateIndex(
                name: "IX_receta_detalle_id_receta",
                table: "receta_detalle",
                column: "id_receta");

            migrationBuilder.CreateIndex(
                name: "IX_resultado_tratante_id_diagnostico",
                table: "resultado_tratante",
                column: "id_diagnostico");

            migrationBuilder.CreateIndex(
                name: "IX_resultado_tratante_id_estudio",
                table: "resultado_tratante",
                column: "id_estudio");

            migrationBuilder.CreateIndex(
                name: "IX_resultado_tratante_id_medico",
                table: "resultado_tratante",
                column: "id_medico");

            migrationBuilder.CreateIndex(
                name: "IX_rol_nombre",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_correo",
                table: "usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_id_medico",
                table: "usuario",
                column: "id_medico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_id_rol",
                table: "usuario",
                column: "id_rol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "facturacion_detalle");

            migrationBuilder.DropTable(
                name: "kardex");

            migrationBuilder.DropTable(
                name: "receta_detalle");

            migrationBuilder.DropTable(
                name: "resultado_tratante");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "facturacion");

            migrationBuilder.DropTable(
                name: "insumo");

            migrationBuilder.DropTable(
                name: "receta");

            migrationBuilder.DropTable(
                name: "estudio");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "asociado_seguro");

            migrationBuilder.DropTable(
                name: "proveedor");

            migrationBuilder.DropTable(
                name: "diagnostico");

            migrationBuilder.DropTable(
                name: "tipo_cobertura");

            migrationBuilder.DropTable(
                name: "cita");

            migrationBuilder.DropTable(
                name: "historial_clinico");

            migrationBuilder.DropTable(
                name: "medico");

            migrationBuilder.DropTable(
                name: "paciente");
        }
    }
}
