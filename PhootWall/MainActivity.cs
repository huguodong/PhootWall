using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace PhootWall
{
    [Activity(Label = "PhootWall", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;
        private GridView mPhotoWall;
        private PhotoWallAdapter adapter;  
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            mPhotoWall = (GridView)FindViewById(Resource.Id.photo_wall);
            adapter = new PhotoWallAdapter(this, 0, Images.imageThumbUrls, mPhotoWall);
            mPhotoWall.Adapter=adapter;  
 
        
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            adapter.cancelAllTasks();  
        }
    }
}

