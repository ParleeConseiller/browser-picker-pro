// Browser Picker Pro — Created by Parlee Conseiller, Inc.
namespace BrowserPickerPro.Core.Interfaces;

public interface IProtocolRegistrar
{
    Task<bool> RegisterAsync(CancellationToken cancellationToken = default);
    Task<bool> UnregisterAsync(CancellationToken cancellationToken = default);
    Task<bool> IsRegisteredAsync(CancellationToken cancellationToken = default);
}
