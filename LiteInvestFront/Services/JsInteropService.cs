using LiteInvest.Entity.PlazaEntity;
using Microsoft.JSInterop;

namespace LiteInvestFront.Services;

public class JsInteropService : IDisposable
{
	public DotNetObjectReference<JsInteropService> ServiceObjectDotNetReference { get; set; }
	//public event Action<string, string, bool, bool, bool> OnKeyDown;
	public event Action<string> OnInitializedWindows;
    public event Action<string, int> OnInitializedQuote;
    public event Action<string> OnAcceptedQuoteData;
    public event Action<string> OnAcceptedClusterData;
    public event Action<string> OnAcceptedChartData;
    //public event Action<string, double, double> OnScaleQuote;
    public event Action<string, bool> OnToggleAutomicCentering;

    public JsInteropService()
	{
		ServiceObjectDotNetReference = DotNetObjectReference.Create(this);
	}

    //[JSInvokable("KeyDown")]
    //public void OnKeyDownJS(string keyCode, string windowId, bool isCtrl, bool isShift, bool isMouseOverQuoteContainer)
    //{
    //	OnKeyDown?.Invoke(keyCode, windowId, isCtrl, isShift, isMouseOverQuoteContainer);
    //}

    [JSInvokable("OnInitializedWindows")]
    public void OnInitializedWindowsJs(string windowId)
    {
        OnInitializedWindows?.Invoke(windowId);
    }

    [JSInvokable("OnInitializedQuote")]
    public void OnInitializedQuoteJs(string windowId, int scrollIndex)
    {
        OnInitializedQuote?.Invoke(windowId, scrollIndex);
    }

    [JSInvokable("OnAcceptedQuoteData")]
    public void OnAcceptedQuoteDataJs(string windowId)
    {
        OnAcceptedQuoteData?.Invoke(windowId);
    }

    [JSInvokable("OnAcceptedClusterData")]
    public void OnAcceptedClusterDataJs(string windowId)
    {
        OnAcceptedClusterData?.Invoke(windowId);
    }

    [JSInvokable("OnAcceptedChartData")]
    public void OnAcceptedChartDataJs(string windowId)
    {
        OnAcceptedChartData?.Invoke(windowId);
    }

    [JSInvokable("OnToggleAutomicCentering")]
    public void OnToggleAutomicCenteringJs(string windowId, bool on)
    {
        OnToggleAutomicCentering?.Invoke(windowId, on);
    }

    public void Dispose()
	{
		ServiceObjectDotNetReference?.Dispose();
	}
}
