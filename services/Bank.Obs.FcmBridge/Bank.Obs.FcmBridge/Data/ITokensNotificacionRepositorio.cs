using System.Collections.Generic;
using System.Threading.Tasks;
using Bank.Obs.FcmBridge.Models;

namespace Bank.Obs.FcmBridge.Data;

public interface ITokensNotificacionRepositorio
{
    Task<IReadOnlyList<TokenNotificacionUsuario>> obtenerTokensPorCodigosAgendaAsync(IReadOnlyList<UsuarioNotificacion> usuarios);
}
