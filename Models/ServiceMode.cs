namespace SoapUI.Models
{
    /// <summary>
    /// Which wire protocol / endpoint flavour to talk to.
    /// RCC listens on a configurable port, RBXGS is fixed at
    /// /RBXGS/WebService.dll on port 80.
    /// </summary>
    public enum ServiceMode
    {
        Rcc,
        Rbxgs
    }
}
