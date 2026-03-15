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
                textoRecibido = req.Message.Text;
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

            if (!_sesiones.TryGetValue(chatId, out var sesion))
            {
                sesion = new SessionStateTelegram { Paso = "INICIO" };
                _sesiones[chatId] = sesion;
            }

            object respuestaParaN8n = null;

            if (sesion.Paso == "INICIO")
            {
                if (botonApretado == "btn_ver_turnos")
                {
                    var turnos = await _db.Turnos.ToListAsync();
                    string textoTurnos = "📅 *Turnos registrados:*\n";

                    if (turnos.Count == 0) textoTurnos += "No hay turnos actualmente.";
                    else
                    {
                        for (int i = 0; i < turnos.Count; i++)
                        {
                            textoTurnos += $"{i + 1}. Turno #{turnos[i].Id}\n";
                        }
                    }

                    respuestaParaN8n = CrearTextoTelegram(chatId, textoTurnos);
                }
                else if (botonApretado == "btn_nuevo_cliente")
                {
                    sesion.Paso = "ESPERANDO_NOMBRE_DUENO";
                    respuestaParaN8n = CrearTextoTelegram(chatId, "¡Excelente! Vamos a registrar un nuevo cliente. 📝\n\nPor favor, decime el *Nombre y Apellido* del dueño:");
                }
                else
                {
                    respuestaParaN8n = CrearMenuPrincipalTelegram(chatId);
                }
            }
            else if (sesion.Paso == "ESPERANDO_NOMBRE_DUENO")
            {
                if (!string.IsNullOrEmpty(textoRecibido))
                {
                    sesion.NombreDueno = textoRecibido;
                    sesion.Paso = "ESPERANDO_NOMBRE_MASCOTA";
                    respuestaParaN8n = CrearTextoTelegram(chatId, $"Perfecto, registré a {sesion.NombreDueno}.\n\nAhora, ¿cómo se llama la *mascota*? 🐾");
                }
            }
            else if (sesion.Paso == "ESPERANDO_NOMBRE_MASCOTA")
            {
                if (!string.IsNullOrEmpty(textoRecibido))
                {
                    sesion.NombreMascota = textoRecibido;

                    try
                    {
                        var partesNombre = sesion.NombreDueno.Split(' ', 2);
                        string nombreDueno = partesNombre[0];
                        string apellidoDueno = partesNombre.Length > 1 ? partesNombre[1] : "";
                        string telefonoFalso = "Telegram-" + chatId;

                        var resultadoDueno = await _db.Dueno
                            .FromSqlRaw("EXEC sp_InsertarDueno @Nombre={0}, @Apellido={1}, @Telefono={2}",
                            nombreDueno, apellidoDueno, telefonoFalso)
                            .ToListAsync();

                        var duenoCreado = resultadoDueno.FirstOrDefault();

                        if (duenoCreado != null)
                        {
                            await _db.Mascotas
                                .FromSqlRaw("EXEC sp_InsertarMascota @nombre={0}, @Raza={1}, @Edad={2}, @Peso={3}, @DuenoId={4}",
                                sesion.NombreMascota, "Mestizo", 0, 0, duenoCreado.Id)
                                .ToListAsync();

                            respuestaParaN8n = CrearTextoTelegram(chatId, $"¡Listo! 🎉 He registrado a {sesion.NombreDueno} y su mascota {sesion.NombreMascota} en la base de datos.\n\nEscribe 'Hola' para volver al menú principal.");
                        }
                        else
                        {
                            respuestaParaN8n = CrearTextoTelegram(chatId, "Hubo un error al guardar el dueño. Intenta nuevamente escribiendo 'Hola'.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error SQL en Telegram: {ex.Message}");
                        respuestaParaN8n = CrearTextoTelegram(chatId, "Ocurrió un error interno al guardar los datos. 😥");
                    }

                    _sesiones[chatId] = new SessionStateTelegram { Paso = "INICIO" };
                }
            }

            if (respuestaParaN8n == null)
            {
                respuestaParaN8n = CrearMenuPrincipalTelegram(chatId);
            }

            return Ok(respuestaParaN8n);
        }

        private object CrearTextoTelegram(long chatId, string texto)
        {
            return new
            {
                chat_id = chatId,
                text = texto,
                parse_mode = "Markdown"
            };
        }

        private object CrearMenuPrincipalTelegram(long chatId)
        {
            return new
            {
                chat_id = chatId,
                text = "¡Hola! Soy el asistente de VetExa 🐾.\n¿Qué necesitas hacer hoy?",
                reply_markup = new
                {
                    inline_keyboard = new[]
                    {
                        new[] { new { text = "📅 Ver Turnos", callback_data = "btn_ver_turnos" } },
                        new[] { new { text = "➕ Nuevo Cliente", callback_data = "btn_nuevo_cliente" } }
                    }
                }
            };
        }
    }

    public class TelegramUpdate
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public TelegramMessage Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("callback_query")]
        public TelegramCallbackQuery CallbackQuery { get; set; }
    }

    public class TelegramMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("chat")]
        public TelegramChat Chat { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class TelegramCallbackQuery
    {
        [System.Text.Json.Serialization.JsonPropertyName("from")]
        public TelegramChat From { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string Data { get; set; }
    }

    public class TelegramChat
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class SessionStateTelegram
    {
        public string Paso { get; set; }
        public string NombreDueno { get; set; }
        public string NombreMascota { get; set; }
    }
}