package phootwall;


public class PhotoWallAdapter_ViewData
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer
{
	static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("PhootWall.PhotoWallAdapter/ViewData, PhootWall, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", PhotoWallAdapter_ViewData.class, __md_methods);
	}


	public PhotoWallAdapter_ViewData () throws java.lang.Throwable
	{
		super ();
		if (getClass () == PhotoWallAdapter_ViewData.class)
			mono.android.TypeManager.Activate ("PhootWall.PhotoWallAdapter/ViewData, PhootWall, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
