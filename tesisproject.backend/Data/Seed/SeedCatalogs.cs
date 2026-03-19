using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Seed
{
    public static class SeedCatalogs
    {
        public static async Task InitializeAsync(AppDbContext db)
        {
            Console.WriteLine("==> Iniciando carga de catálogos...");

            await SeedAcademicTermsAsync(db);
            await SeedIndexingSourcesAsync(db);
            await SeedOcdeFieldsAsync(db);
            await SeedPublicationStatusesAsync(db);
            await SeedResearchLinesAsync(db);

            Console.WriteLine("==> Catálogos cargados correctamente ✅");
        }

        // =========================================================
        // 1. Período Académico
        // =========================================================
        private static async Task SeedAcademicTermsAsync(AppDbContext db)
        {
            var terms = new[]
            {
                "Febrero - Marzo 2020",
                "Abril - Septiembre 2021",
                "Octubre 2021 - Marzo 2022"
            };

            foreach (var name in terms)
            {
                if (!await db.AcademicTerms.AnyAsync(x => x.Name == name))
                {
                    db.AcademicTerms.Add(new AcademicTerm { Name = name });
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine("   [OK] AcademicTerms");
        }

        // =========================================================
        // 2. Base de datos indexado -> IndexingSource
        // =========================================================
        private static async Task SeedIndexingSourcesAsync(AppDbContext db)
        {
            var sources = new[]
            {
                "ISI WEB OF KNOWLEDGE",
                "SCIMAGO JOURNAL RANK",
                "LATIN INDEX",
                "LILACS",
                "SCIELO",
                "REDALYC",
                "EBSCO",
                "JSTOR",
                "OAJI",
                "DOAJ",
                "CINAHL",
                "SCOPUS",
                "OPEN_JOURNAL_SYSTEMS",
                "ISI_JOURNAL",
                "HEIN_ON_LINE_LIBRARY",
                "CHEMICAL_ABSTRACTS_INDIAN_CITATION_INDEX",
                "ERIHPLUS",
                "WEB_OF_CIENCE"
            };

            foreach (var name in sources)
            {
                if (!await db.IndexingSources.AnyAsync(x => x.Name == name))
                {
                    db.IndexingSources.Add(new IndexingSource
                    {
                        Name = name,
                        IsActive = true
                    });
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine("   [OK] IndexingSources");
        }

        // =========================================================
        // 3. OCDE / Campos Amplios, Específicos, Detallados
        // =========================================================
        private static async Task SeedOcdeFieldsAsync(AppDbContext db)
        {
            // Estructura tomada de tu TXT/XLSX
            var structure = new[]
            {
                new
                {
                    BroadKey = "ARTES_HUMANIDADES",
                    BroadName = "Artes y Humanidades",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "ARTES",
                            SpecName = "Artes",
                            Details = new (string Code, string Name)[]
                            {
                                ("1-12A", "Técnicas audiovisuales y producción para medios de comunicación"),
                                ("2-12A", "Diseño"),
                                ("3-12A", "Artes"),
                                ("5-12A", "Música y artes escénicas")
                            }
                        },
                        new {
                            SpecKey = "HUMANIDADES",
                            SpecName = "Humanidades",
                            Details = new (string, string)[]
                            {
                                ("1-22A", "Religión y Teología"),
                                ("2-22A", "Historia y Arqueología"),
                                ("3-22A", "Filosofía")
                            }
                        },
                        new {
                            SpecKey = "IDIOMAS",
                            SpecName = "Idiomas",
                            Details = new (string, string)[]
                            {
                                ("1-32A", "Idiomas"),
                                ("2-32A", "Literatura y lingüística")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "CSOCIALES_PERIODISMO_DERECHO",
                    BroadName = "Ciencias Sociales, Periodismo, Información y Derecho",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "CSOCIALES_COMPORTAMIENTO",
                            SpecName = "Ciencias sociales y del comportamiento",
                            Details = new (string, string)[]
                            {
                                ("1-13A", "Economía"),
                                ("81-13A", "Economía Matemática"),
                                ("2-13A", "Ciencias políticas"),
                                ("3-13A", "Psicología"),
                                ("4-13A", "Estudios Sociales y Culturales"),
                                ("82-13A", "Estudios de Género"),
                                ("83-13A", "Geografía y Territorio")
                            }
                        },
                        new {
                            SpecKey = "PERIODISMO_INFO",
                            SpecName = "Periodismo e información",
                            Details = new (string, string)[]
                            {
                                ("1-23A", "Periodismo y comunicación"),
                                ("2-23A", "Bibliotecología, documentación y archivología")
                            }
                        },
                        new {
                            SpecKey = "DERECHO",
                            SpecName = "Derecho",
                            Details = new (string, string)[]
                            {
                                ("1-33A", "Derecho")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "ADMINISTRACION",
                    BroadName = "Administración",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "EDU_COMERCIAL_ADMIN",
                            SpecName = "Educación comercial y administración",
                            Details = new (string, string)[]
                            {
                                ("1-14A", "Contabilidad y auditoría"),
                                ("2-14A", "Gestión financiera"),
                                ("3-14A", "Administración"),
                                ("4-14A", "Mercadotecnia y publicidad"),
                                ("5-14A", "Información gerencial"),
                                ("6-14A", "Comercio"),
                                ("7-14A", "Competencias laborales")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "CNATURALES_MAT_EST",
                    BroadName = "Ciencias Naturales, Matemáticas y Estadísticas",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "CBIOLOGICAS",
                            SpecName = "Ciencias biológicas y afines",
                            Details = new (string, string)[]
                            {
                                ("1-15A", "Biología"),
                                ("81-15A", "Biofísica"),
                                ("82-15A", "Biofarmacéutica"),
                                ("83-15A", "Biomedicina"),
                                ("2-15A", "Bioquímica"),
                                ("84-15A", "Genética"),
                                ("85-15A", "Biodiversidad"),
                                ("86-15A", "Neurociencias")
                            }
                        },
                        new {
                            SpecKey = "MEDIO_AMBIENTE",
                            SpecName = "Medio ambiente",
                            Details = new (string, string)[]
                            {
                                ("1-25A", "Medio ambiente"),
                                ("2-25A", "Recursos Naturales Renovables")
                            }
                        },
                        new {
                            SpecKey = "CFISICAS",
                            SpecName = "Ciencias físicas",
                            Details = new (string, string)[]
                            {
                                ("1-35A", "Química"),
                                ("2-35A", "Ciencias de la Tierra"),
                                ("3-35A", "Física")
                            }
                        },
                        new {
                            SpecKey = "MAT_EST",
                            SpecName = "Matemáticas y estadística",
                            Details = new (string, string)[]
                            {
                                ("1-45A", "Matemáticas"),
                                ("2-45A", "Estadísticas"),
                                ("81-45A", "Logística y transporte")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "TIC",
                    BroadName = "Tecnologías de la Información y la Comunicación (TIC)",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "TIC",
                            SpecName = "Tecnologías de la Información y la Comunicación (TIC)",
                            Details = new (string, string)[]
                            {
                                ("1-16A", "Computación"),
                                ("2-16A", "Diseño y administración de redes y bases de datos"),
                                ("3-16A", "Desarrollo y análisis de software y aplicaciones"),
                                ("81-16A", "Sistemas de información")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "ING_IND_CONST",
                    BroadName = "Ingeniería, Industria y Construcción",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "ING_PROF_AF",
                            SpecName = "Ingeniería y profesiones afines",
                            Details = new (string, string)[]
                            {
                                ("1-17A", "Química aplicada"),
                                ("2-17A", "Tecnología de protección del medio ambiente"),
                                ("3-17A", "Electricidad y energía"),
                                ("4-17A", "Electrónica, automatización y sonido"),
                                ("5-17A", "Mecánica y profesiones afines"),
                                ("6-17A", "Diseño y construcción de vehículos, barcos y aeronaves motorizadas"),
                                ("81-17A", "Tecnologías nucleares y energéticas"),
                                ("82-17A", "Mecatrónica"),
                                ("83-17A", "Hidráulica"),
                                ("84-17A", "Telecomunicaciones"),
                                ("85-17A", "Nanotecnología")
                            }
                        },
                        new {
                            SpecKey = "INDUSTRIA_PROD",
                            SpecName = "Industria y producción",
                            Details = new (string, string)[]
                            {
                                ("1-27A", "Procesamiento de alimentos"),
                                ("2-27A", "Materiales"),
                                ("3-27A", "Productos textiles"),
                                ("4-27A", "Minería y extracción"),
                                ("5-27A", "Producción industrial"),
                                ("6-27A", "Seguridad industrial"),
                                ("7-27A", "Diseño industrial y de procesos"),
                                ("82-7A", "Mantenimiento industrial")
                            }
                        },
                        new {
                            SpecKey = "ARQ_CONS",
                            SpecName = "Arquitectura y construcción",
                            Details = new (string, string)[]
                            {
                                ("1-37A", "Arquitectura, urbanismo y restauración"),
                                ("2-37A", "Construcción e ingeniería civil")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "AGRI_SILV_PES_VET",
                    BroadName = "Agricultura, Silvicultura, Pesca y Veterinaria",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "AGRICULTURA",
                            SpecName = "Agricultura",
                            Details = new (string, string)[]
                            {
                                ("1-18A", "Producción agrícola y ganadera")
                            }
                        },
                        new {
                            SpecKey = "SILVICULTURA",
                            SpecName = "Silvicultura",
                            Details = new (string, string)[]
                            {
                                ("1-28A", "Silvicultura")
                            }
                        },
                        new {
                            SpecKey = "PESCA",
                            SpecName = "Pesca",
                            Details = new (string, string)[]
                            {
                                ("1-38A", "Pesca")
                            }
                        },
                        new {
                            SpecKey = "VETERINARIA",
                            SpecName = "Veterinaria",
                            Details = new (string, string)[]
                            {
                                ("1-48A", "Veterinaria")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "SALUD_BIENESTAR",
                    BroadName = "Salud y Bienestar",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "SALUD",
                            SpecName = "Salud",
                            Details = new (string, string)[]
                            {
                                ("1-19A", "Odontología"),
                                ("2-19A", "Medicina"),
                                ("3-19A", "Enfermería y obstetricia"),
                                ("4-19A", "Tecnología de diagnóstico y tratamiento médico"),
                                ("5-19A", "Terapia y rehabilitación"),
                                ("6-19A", "Farmacia"),
                                ("7-19A", "Terapias alternativas y complementarias"),
                                ("8-19A", "Salud pública")
                            }
                        },
                        new {
                            SpecKey = "BIENESTAR",
                            SpecName = "Bienestar",
                            Details = new (string, string)[]
                            {
                                ("1-29A", "Asistencia a adultos mayores y discapacitados"),
                                ("2-29A", "Asistencia a la infancia y servicios para jóvenes")
                            }
                        }
                    }
                },
                new
                {
                    BroadKey = "SERVICIOS",
                    BroadName = "Servicios",
                    Specifics = new[]
                    {
                        new {
                            SpecKey = "SERV_PERSONALES",
                            SpecName = "Servicios personales",
                            Details = new (string, string)[]
                            {
                                ("2-110A", "Peluquería y tratamiento de belleza"),
                                ("3-110A", "Hotelería y gastronomía"),
                                ("4-110A", "Actividad física"),
                                ("5-110A", "Turismo")
                            }
                        },
                        new {
                            SpecKey = "SERV_PROTECCION",
                            SpecName = "Servicios de protección",
                            Details = new (string, string)[]
                            {
                                ("1-210A", "Prevención y gestión de riesgos"),
                                ("2-210A", "Salud y seguridad ocupacional")
                            }
                        },
                        new {
                            SpecKey = "SERV_SEGURIDAD",
                            SpecName = "Servicios de seguridad",
                            Details = new (string, string)[]
                            {
                                ("1-310A", "Educación policial, militar y defensa"),
                                ("2-310A", "Seguridad ciudadana")
                            }
                        },
                        new {
                            SpecKey = "SERV_TRANSPORTE",
                            SpecName = "Servicio de transporte",
                            Details = new (string, string)[]
                            {
                                ("1-410A", "Gestión del transporte")
                            }
                        }
                    }
                }
            };

            // ---------- 3.1 BroadFields ----------
            var broadMap = new Dictionary<string, BroadField>();

            foreach (var b in structure)
            {
                var broad = await db.BroadFields
                    .FirstOrDefaultAsync(x => x.Name == b.BroadName);

                if (broad == null)
                {
                    broad = new BroadField
                    {
                        Name = b.BroadName
                    };
                    db.BroadFields.Add(broad);
                }

                broadMap[b.BroadKey] = broad;
            }

            await db.SaveChangesAsync();
            Console.WriteLine("   [OK] BroadFields");

            // ---------- 3.2 SpecificFields ----------
            var specificMap = new Dictionary<string, SpecificField>();

            foreach (var b in structure)
            {
                var broad = await db.BroadFields
                    .FirstAsync(x => x.Name == b.BroadName);

                // Asumimos PK: BroadFieldId
                foreach (var s in b.Specifics)
                {
                    var spec = await db.SpecificFields
                        .FirstOrDefaultAsync(x =>
                            x.BroadFieldId == broad.BroadFieldId &&
                            x.Name == s.SpecName);

                    if (spec == null)
                    {
                        spec = new SpecificField
                        {
                            Name = s.SpecName,
                            BroadFieldId = broad.BroadFieldId
                        };
                        db.SpecificFields.Add(spec);
                    }

                    specificMap[s.SpecKey] = spec;
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine("   [OK] SpecificFields");

            // ---------- 3.3 DetailedFields ----------
            foreach (var b in structure)
            {
                foreach (var s in b.Specifics)
                {
                    var spec = await db.SpecificFields
                        .FirstAsync(x => x.Name == s.SpecName);

                    // Asumimos PK: SpecificFieldId
                    foreach (var d in s.Details)
                    {
                        var exists = await db.DetailedFields
                            .AnyAsync(x =>
                                x.SpecificFieldId == spec.SpecificFieldId &&
                                x.Name == d.Name);

                        if (!exists)
                        {
                            db.DetailedFields.Add(new DetailedField
                            {
                                Name = d.Name,
                                Code = d.Code,
                                SpecificFieldId = spec.SpecificFieldId
                            });
                        }
                    }
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine("   [OK] DetailedFields");
        }

        // =========================================================
        // 4. Estado de publicación
        // =========================================================
        private static async Task SeedPublicationStatusesAsync(AppDbContext db)
        {
            var statuses = new[]
            {
        "PUBLICADO",
        "ACEPTADO",
        "SIN ESTADO",
    };

            foreach (var name in statuses)
            {
                if (!await db.PublicationStatuses.AnyAsync(x => x.Name == name))
                {
                    db.PublicationStatuses.Add(new PublicationStatus { Name = name });
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine("   [OK] PublicationStatuses");
        }

        // =========================================================
        // 5. Líneas de investigación
        // =========================================================
        private static async Task SeedResearchLinesAsync(AppDbContext db)
        {
            var lines = new[]
            {
                "COMPORTAMIENTO SOCIAL Y EDUCATIVO",
                "COMUNICACION, SOCIEDAD, CULTURA Y TECNOLOGIA",
                "CONSTRUCCION, ESTRUCTURAS, VIAS Y TRANSPORTE",
                "DESARROLLO EMPRESARIAL",
                "DISEÑO, MATERIALES Y PRODUCCION",
                "ECONOMIA DE DESARROLLO",
                "ENERGIA, DESARROLLO SOSTENIBLE Y GESTION DE RECURSOS NATURALES",
                "EXCLUSION E INTEGRACION SOCIAL",
                "MICROBIOLOGIA Y BIOTECNOLOGIA",
                "POLITICAS PUBLICAS, DERECHO Y SOCIEDAD",
                "PRODUCCION AGROALIMENTARIA Y MEDIO AMBIENTE",
                "SALUD HUMANA",
                "SEGURIDAD Y SOBERANIA ALIMENTARIA",
                "TECNOLOGIA DE LA INFORMACION Y SISTEMAS DE CONTROL",
                "NO APLICA"
            };

            foreach (var name in lines)
            {
                if (!await db.ResearchLines.AnyAsync(x => x.Name == name))
                {
                    db.ResearchLines.Add(new ResearchLine { Name = name });
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine("   [OK] ResearchLines");
        }
    }
}
