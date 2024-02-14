using AForge.Video;

namespace camera.Delegates
{
    public static class DelegateUtils
    {
        public delegate void VideoSource_NewFrame_delegate_EventHandler(object sender, NewFrameEventArgs eventArgs);
        public delegate void Timer_Tick_delegate_EventHandler(object sender, EventArgs e);
    }
}
