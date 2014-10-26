﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FiredTVLauncher
{
    [Activity (Label = "Reorder Apps")]			
    public class ReorderActivity : ListActivity
    {
        ReorderAdapter adapter;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            adapter = new ReorderAdapter {
                Context = this
            };

            ListAdapter = adapter;

            adapter.Reload ();

            ListView.ItemLongClick += (sender, e) => {
                var p = e.Position;

                if (adapter.SelectedPosition >= 0) {
                    adapter.SelectedPosition = -1;
                    adapter.NotifyDataSetChanged ();
                }

                Console.WriteLine ("LONG PRESS: " + p);

                adapter.SelectedPosition = p;
                adapter.NotifyDataSetChanged ();

                Toast.MakeText (this, "Move the item up and down, click again when done!", ToastLength.Long).Show ();

            };
            ListView.ItemClick += (sender, e) => {

                adapter.SelectedPosition = -1;
                adapter.NotifyDataSetChanged ();
            };

            Toast.MakeText (this, "Select the item you want to move, then long click enter...", ToastLength.Long).Show ();

        }

        public override bool OnKeyUp (Keycode keyCode, KeyEvent e)
        {
            base.OnKeyUp (keyCode, e);

            if (adapter.SelectedPosition >= 0) {

                if (e.KeyCode == Keycode.DpadUp) {
                    Console.WriteLine ("ORDER UP");
                    adapter.Reorder (adapter.SelectedPosition, true);
                    ListView.SetSelection (adapter.SelectedPosition);
                    ScrollListView ();
                    return true;
                } else if (e.KeyCode == Keycode.DpadDown) {
                    Console.WriteLine ("ORDER DOWN");
                    adapter.Reorder (adapter.SelectedPosition, false);
                    ListView.SetSelection (adapter.SelectedPosition);
                    ScrollListView ();
                    return true;
                }
            }

            return false;
        }

        void ScrollListView ()
        {
            int listViewHeight = ListView.Height;

            var reorderItemLayoutHeight = 
                Android.Util.TypedValue.ApplyDimension (Android.Util.ComplexUnitType.Dip, 50, Resources.DisplayMetrics);

            ListView.SetSelectionFromTop (adapter.SelectedPosition, (int) (listViewHeight / 2 - reorderItemLayoutHeight / 2));
        }
    }

    public class ReorderAdapter : BaseAdapter<AppInfo>
    {
        public ReorderAdapter ()
        {
            SelectedPosition = -1;
            Apps = new List<AppInfo> ();
        }

        public Activity Context { get; set; }

        public List<AppInfo> Apps { get; set; }

        public int SelectedPosition { get; set; }

        public override long GetItemId (int position) { return position; } 
        public override int Count { get { return Apps.Count; } }
        public override AppInfo this [int index] { get { return Apps [index]; } }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            bool reused = false;
            var app = Apps [position];
            var view = convertView ??
                        LayoutInflater.FromContext (Context).Inflate (Resource.Layout.ReorderItemLayout, parent, false);
            
            view.FindViewById<TextView> (Resource.Id.textName).Text = app.Name;
            view.FindViewById<ImageView> (Resource.Id.imageIcon).SetImageDrawable (app.GetIcon (Context));

//            view.FindViewById<ImageButton> (Resource.Id.buttonUp).Click += (sender, e) => {
//                Console.WriteLine ("UP");
//            };
//            view.FindViewById<ImageButton> (Resource.Id.buttonDown).Click += (sender, e) => {
//                Console.WriteLine ("DOWN");
//            };           

            if (SelectedPosition == position)
                view.FindViewById<ImageView> (Resource.Id.imageReorder).Visibility = ViewStates.Visible;
            else
                view.FindViewById<ImageView> (Resource.Id.imageReorder).Visibility = ViewStates.Invisible;

            return view;
        }

        public void Sort () 
        {
            Apps.Sort ((a1, a2) => Settings.Instance.GetAppOrder(a1.PackageName).Order.CompareTo(Settings.Instance.GetAppOrder(a2.PackageName).Order));
        }
            
        public void Reorder (int position, bool up)
        {
            var app = Apps [position];
            Settings.Instance.MoveOrder (app.PackageName, up);

            Sort ();

            if (up)
                SelectedPosition = SelectedPosition - 1;
            else
                SelectedPosition = SelectedPosition + 1;

            if (SelectedPosition <= 0)
                SelectedPosition = 0;

            if (SelectedPosition > Apps.Count - 1)
                SelectedPosition = Apps.Count - 1;
                
            NotifyDataSetChanged ();
        }

        public void Reload () 
        {
            AppInfo.FetchApps (Context, false, r => {

                Apps.Clear ();
                Apps.AddRange (r);

                Settings.Instance.SanitizeAppOrder (Apps);

                Sort ();

                Context.RunOnUiThread (NotifyDataSetChanged);
            });
        }
    }
}

