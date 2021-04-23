
namespace LEd {

[System.Serializable]
public class ProjectJSON {
	public Header __header__;
	public int jsonVersion;
	public float defaultPivotX;
	public float defaultPivotY;
	public int defaultGridSize;
	public string bgColor;
	public int nextUid;
	public Defs defs;
	public Level[] levels;
}

[System.Serializable]
public class Header {
	public string fileType;
	public string app;
	public string appAuthor;
	public string appVersion;
	public string url;
}

}