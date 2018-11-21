public class ModWidget : Widget
{
	private KMWidget _modWidget;

	private void Awake()
	{
		_modWidget = GetComponent<KMWidget>();
		SizeX = _modWidget.SizeX;
		SizeZ = _modWidget.SizeZ;
	}

	public override void Activate()
	{
		if (_modWidget.OnWidgetActivate != null)
			_modWidget.OnWidgetActivate.Invoke();
	}

	public override string GetResult(string key, string data)
	{
		if (_modWidget.OnQueryRequest != null)
			return _modWidget.OnQueryRequest.Invoke(key, data);
		return null;
	}
}