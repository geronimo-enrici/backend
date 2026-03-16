using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using prueba.Models;
using MascotasApi;

namespace prueba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramController : ControllerBase
    {
        private static readonly Dictionary<long, SessionStateTelegram> _sesiones = new();
        private readonly AppDbContext _db;

        public TelegramController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] TelegramUpdate req)
        {
            long chatId = 0;
            string textoRecibido = "";
            string botonApretado = "";

            if (req.Message != null)
            {
                chatId = req.Message.Chat.Id;
                textoRecibido = req.Message.Text ?? "";
            }
            else if (req.CallbackQuery != null)
            {
                chatId = req.CallbackQuery.From.Id;
                botonApretado = req.CallbackQuery.Data;
            }
            else
            {
                return Ok();
            }

            // Recuperar o crear sesión
            if (!_sesiones.TryGetValue(chatId, out var sesion))
            {
                sesion = new SessionStateTelegram { Paso = "INICIO" };
                _sesiones[chatId] = sesion;
            }

            // Solo reseteamos a INICIO si tocamos un botón del menú principal
            if (botonApretado == "btn_ver_turnos" || botonApretado == "btn_ver_mascotas" || botonApretado == "btn_nuevo_cliente")
            {
                sesion.Paso = "INICIO";
            }

            object respuestaParaN8n = null;

            // --- SISTEMA DE LINKS A MASCOTAS (/pet_ID) ---
            if (!string.IsNullOrEmpty(textoRecibido) && textoRecibido.Trim().StartsWith("/pet_"))
            {
                string idStr = textoRecibido.Trim().Replace("/pet_", "");
                if (int.TryParse(idStr, out int mascotaId))
                {
                    var mascota = await _db.Mascotas
                        .Include(m => m.Dueno)
                        .FirstOrDefaultAsync(m => m.Id == mascotaId);

                    if (mascota != null)
                    {
                        string duenoNombre = mascota.Dueno != null ? $"{mascota.Dueno.Nombre}" : "Sin dueño asignado";
                        string detalle = $"🐾 *FICHA CLÍNICA: {mascota.nombre.ToUpper()}*\n";
                        detalle += $"──────────────\n";
                        detalle += $"🔹 *Raza:* {mascota.Raza}\n";
                        detalle += $"🔹 *Edad:* {mascota.Edad} años\n";
                        detalle += $"🔹 *Peso:* {mascota.Peso} kg\n";
                        detalle += $"👤 *Dueño:* {duenoNombre}\n\n";
                        detalle += "Escribe *Hola* para volver al menú principal.";

                        respuestaParaN8n = CrearTextoTelegram(chatId, detalle);
                    }
                    else
                    {
                        respuestaParaN8n = CrearTextoTelegram(chatId, "❌ No encontré esa mascota en el sistema. Escribe *Hola* para volver.");
                    }
                }
                sesion.Paso = "INICIO";
            }
            // ---------------------------------------------

            else if (sesion.Paso == "INICIO")
            {
                if (botonApretado == "btn_ver_turnos")
                {
                    // FILTRO: Solo los turnos de HOY
                    DateTime hoy = DateTime.Today;
                    var turnosDeHoy = await _db.Turnos
                        .Where(t => t.FechaHora.Date == hoy)
                        .OrderBy(t => t.FechaHora)
                        .ToListAsync();

                    string textoTurnos = $"📅 *TURNOS DE HOY ({hoy:dd/MM/yyyy})*\n\n";

                    if (turnosDeHoy.Count == 0)
                    {
                        textoTurnos += "¡Día libre! No hay turnos programados para hoy.";
                    }
                    else
                    {
                        foreach (var t in turnosDeHoy)
                        {
                            var mascotaRelacionada = await _db.Mascotas
                                .FirstOrDefaultAsync(m => m.nombre.ToLower() == t.MascotaNombre.ToLower());

                            string linkMascota = mascotaRelacionada != null
                                ? $"🔗 *Ver ficha:* /pet_{mascotaRelacionada.Id}"
                                : "⚠️ *(No registrada en sistema)*";

                            textoTurnos += $"🕒 *{t.FechaHora:HH:mm} hs*\n";
                            textoTurnos += $"🐾 *Paciente:* {t.MascotaNombre}\n";
                            textoTurnos += $"🩺 *Motivo:* {t.Tipo}\n";
                            textoTurnos += $"{linkMascota}\n"; // El link va en su propia línea
                            textoTurnos += "──────────────\n";
                        }
                    }
                    respuestaParaN8n = CrearTextoTelegram(chatId, textoTurnos);
                }
                else if (botonApretado == "btn_ver_mascotas")
                {
                    var mascotas = await _db.Mascotas.Include(m => m.Dueno).ToListAsync();
                    string textoMascotas = "🐕 *MASCOTAS REGISTRADAS*\n\n";

                    if (mascotas.Count == 0)
                    {
                        textoMascotas += "Aún no hay mascotas en la base de datos.";
                    }
                    else
                    {
                        foreach (var m in mascotas)
                        {
                            string duenoNombre = m.Dueno != null ? $"{m.Dueno.Nombre}" : "Sin dueño";
                            textoMascotas += $"▪️ *{m.nombre}* ({m.Raza}) - 👤 {duenoNombre}\n";
                            textoMascotas += $"   🔍 Detalle: /pet_{m.Id}\n\n";
                        }
                    }
                    respuestaParaN8n = CrearTextoTelegram(chatId, textoMascotas);
                }
                else if (botonApretado == "btn_nuevo_cliente")
                {
                    sesion.Paso = "ESPERANDO_NOMBRE";
                    respuestaParaN8n = CrearTextoTelegram(chatId, "✍️ ¡Vamos a registrar un nuevo cliente!\n\nPor favor, decime el *Nombre*:");
                }
                else if (!string.IsNullOrEmpty(textoRecibido) && respuestaParaN8n == null)
                {
                    respuestaParaN8n = CrearMenuPrincipalTelegram(chatId);
                }
            }
            else if (sesion.Paso == "ESPERANDO_NOMBRE")
            {
                if (!string.IsNullOrEmpty(textoRecibido))
                {
                    sesion.NombreDueno = textoRecibido;
                    sesion.Paso = "ESPERANDO_APELLIDO";
                    respuestaParaN8n = CrearTextoTelegram(chatId, $"Genial. El nombre es *{sesion.NombreDueno}*.\n\nAhora decime su *Apellido*:");
                }
            }
            else if (sesion.Paso == "ESPERANDO_APELLIDO")
            {
                if (!string.IsNullOrEmpty(textoRecibido))
                {
                    sesion.ApellidoDueno = textoRecibido;
                    sesion.Paso = "ESPERANDO_TELEFONO";
                    respuestaParaN8n = CrearTextoTelegram(chatId, $"Perfecto. Por último, decime su *Celular*:");
                }
            }
            else if (sesion.Paso == "ESPERANDO_TELEFONO")
            {
                if (!string.IsNullOrEmpty(textoRecibido))
                {
                    sesion.Telefono = textoRecibido;
                    sesion.Paso = "ESPERANDO_CONFIRMACION";

                    // Mandamos los botones interactivos de Confirmación
                    respuestaParaN8n = new
                    {
                        chat_id = chatId,
                        text = $"📋 *RESUMEN DEL CLIENTE*\n\n👤 *Nombre:* {sesion.NombreDueno} {sesion.ApellidoDueno}\n📱 *Celular:* {sesion.Telefono}\n\n¿Los datos son correctos?",
                        parse_mode = "Markdown",
                        reply_markup = new
                        {
                            inline_keyboard = new[]
                            {
                                new[]
                                {
                                    new { text = "✅ Confirmar y Guardar", callback_data = "btn_confirmar_cliente" }
                                },
                                new[]
                                {
                                    new { text = "❌ Cancelar", callback_data = "btn_cancelar_cliente" }
                                }
                            }
                        }
                    };
                }
            }
            else if (sesion.Paso == "ESPERANDO_CONFIRMACION")
            {
                if (botonApretado == "btn_confirmar_cliente")
                {
                    try
                    {
                        await _db.Database.ExecuteSqlRawAsync("EXEC sp_InsertarDueno @Nombre={0}, @Apellido={1}, @Telefono={2}",
                            sesion.NombreDueno, sesion.ApellidoDueno, sesion.Telefono);

                        respuestaParaN8n = CrearTextoTelegram(chatId, $"🎉 ¡Excelente! He registrado a *{sesion.NombreDueno} {sesion.ApellidoDueno}* con éxito.\n\nEscribe *Hola* para volver al menú.");
                    }
                    catch (Exception)
                    {
                        respuestaParaN8n = CrearTextoTelegram(chatId, "❌ Hubo un error al guardar en la base de datos. Escribe *Hola* para reiniciar.");
                    }
                    _sesiones[chatId] = new SessionStateTelegram { Paso = "INICIO" };
                }
                else if (botonApretado == "btn_cancelar_cliente")
                {
                    _sesiones[chatId] = new SessionStateTelegram { Paso = "INICIO" };
                    respuestaParaN8n = CrearTextoTelegram(chatId, "🚫 Registro cancelado.\n\nEscribe *Hola* para volver al menú principal.");
                }
            }

            // Si llegamos acá y no hay respuesta, mostramos el menú de error
            if (respuestaParaN8n == null && string.IsNullOrEmpty(botonApretado))
            {
                respuestaParaN8n = CrearMenuPrincipalTelegram(chatId);
            }
            else if (respuestaParaN8n == null)
            {
                respuestaParaN8n = CrearTextoTelegram(chatId, "Lo siento, me confundí. Escribe *Hola* para reiniciar.");
            }

            return Ok(respuestaParaN8n);
        }

        private object CrearTextoTelegram(long chatId, string texto)
        {
            return new { chat_id = chatId, text = texto, parse_mode = "Markdown" };
        }

        private object CrearMenuPrincipalTelegram(long chatId)
        {
            return new
            {
                chat_id = chatId,
                text = "¡Hola! Asistente VetExa 🐾.\n¿Qué necesitas hacer hoy?",
                reply_markup = new
                {
                    inline_keyboard = new[]
                    {
                        new[] { new { text = "📅 Turnos de Hoy", callback_data = "btn_ver_turnos" } },
                        new[] { new { text = "🐾 Ver Mascotas", callback_data = "btn_ver_mascotas" } },
                        new[] { new { text = "➕ Nuevo Cliente", callback_data = "btn_nuevo_cliente" } }
                    }
                }
            };
        }
    }

    // --- CLASES AUXILIARES ---
    public class TelegramUpdate
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public TelegramMessage? Message { get; set; } = null;

        [System.Text.Json.Serialization.JsonPropertyName("callback_query")]
        public TelegramCallbackQuery? CallbackQuery { get; set; } = null;
    }

    public class TelegramMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("chat")]
        public TelegramChat Chat { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class TelegramCallbackQuery
    {
        [System.Text.Json.Serialization.JsonPropertyName("from")]
        public TelegramChat From { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string? Data { get; set; }
    }

    public class TelegramChat
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class SessionStateTelegram
    {
        public string Paso { get; set; } = "INICIO";
        public string NombreDueno { get; set; } = "";
        public string ApellidoDueno { get; set; } = "";
        public string Telefono { get; set; } = "";
    }
}