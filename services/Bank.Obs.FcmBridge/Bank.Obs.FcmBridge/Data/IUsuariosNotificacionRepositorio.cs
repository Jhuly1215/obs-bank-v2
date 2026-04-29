using System.Collections.Generic;
using System.Threading.Tasks;
using Bank.Obs.FcmBridge.Models;
using Bank.Obs.FcmBridge.Models.Requests;

namespace Bank.Obs.FcmBridge.Data;

public interface IUsuariosNotificacionRepositorio
{
    Task registrarUsuarioNotificacionAsync(RegistrarUsuarioNotificacionRequest solicitud, string usuarioOperacion);
    Task cambiarEstadoUsuarioNotificacionAsync(CambiarEstadoUsuarioNotificacionRequest solicitud, string usuarioOperacion);
    Task<IReadOnlyList<UsuarioNotificacion>> listarUsuariosNotificacionActivosAsync();
    Task<IReadOnlyList<UsuarioNotificacion>> obtenerUsuariosNotificacionAsync();
}
