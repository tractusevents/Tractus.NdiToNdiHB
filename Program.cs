using NewTek;
using NewTek.NDI;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Tractus.Ndi;

NDIWrapper.Initialize(false);

Console.WriteLine("Tractus HX to HB for NDI Converter");
Console.WriteLine("Find more of our tools at https://www.tractusevents.com/tools");
Console.WriteLine("-------------------------------------------------------------");
Console.WriteLine("Now available for Discovery registration.");

var inboundVideoFrame = new NDIlib.video_frame_v2_t();
var inboundAudioFrame = new NDIlib.audio_frame_v3_t();

var createSettings = new NDIlib.recv_create_v3_t
{
    allow_video_fields = true,
    bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
    color_format = NDIlib.recv_color_format_e.recv_color_format_fastest,
    p_ndi_recv_name = UTF.StringToUtf8("Tractus HX to HB for NDI Converter"),
};

var receiverPtr = NDIWrapper.recv_create_v3(ref createSettings);
Marshal.FreeHGlobal(createSettings.p_ndi_recv_name);

var advertiserCreateSettings = new NDIlib_recv_advertiser_create_t
{
};
var advertiserPtr = NDIWrapper.recv_advertiser_create(ref advertiserCreateSettings);

NDIWrapper.recv_advertiser_add_receiver(advertiserPtr, receiverPtr, true, true, string.Empty);

nint sendPtr = nint.Zero;

var running = true;

Console.CancelKeyPress += (o, e) =>
{
    running = false;
    e.Cancel = true;
    Console.WriteLine("Received Ctrl+C. Exiting...");
};

var videoFramesDecoded = 0;
var audioFramesDecoded = 0;
var noFramesDecoded = true;

unsafe
{
    while (running)
    {
        var frameType = NDIWrapper.recv_capture_v3(
            receiverPtr,
            &inboundVideoFrame,
            &inboundAudioFrame,
            null,
            1000);

        if (frameType == NDIlib.frame_type_e.frame_type_none)
        {
            continue;
        }

        if(frameType == NDIlib.frame_type_e.frame_type_source_change)
        {
            if(NDIWrapper.recv_get_source_name(receiverPtr, out var pName, 1000))
            {
                if(sendPtr != nint.Zero)
                {
                    NDIWrapper.send_destroy(sendPtr);
                    sendPtr = nint.Zero;
                }

                if (pName != nint.Zero) 
                {
                    var name = UTF.Utf8ToString(pName);
                    Console.WriteLine($"Connected source changed to '{name}'");
                    NDIWrapper.recv_free_string(receiverPtr, pName);

                    var sendSettings = new NDIlib.send_create_t
                    {
                        clock_audio = false,
                        clock_video = false,
                        p_ndi_name = UTF.StringToUtf8($"HX to HB ({name})")
                    };

                    sendPtr = NDIWrapper.send_create(ref sendSettings);
                }
            }

            continue;
        }

        if (noFramesDecoded)
        {
            Console.WriteLine("Now receiving frames!");
            noFramesDecoded = false;
        }

        if (frameType == NDIlib.frame_type_e.frame_type_video)
        {
            if(sendPtr != nint.Zero)
            {
                NDIWrapper.send_send_video_v2(sendPtr, ref inboundVideoFrame);
            }

            NDIWrapper.recv_free_video_v2(receiverPtr, ref inboundVideoFrame);
            videoFramesDecoded++;
        }

        if (frameType == NDIlib.frame_type_e.frame_type_audio)
        {
            if (sendPtr != nint.Zero)
            {
                NDIWrapper.send_send_audio_v3(sendPtr, ref inboundAudioFrame);
            }

            NDIWrapper.recv_free_audio_v3(receiverPtr, ref inboundAudioFrame);
            audioFramesDecoded++;
        }
    }
}


Console.WriteLine($"Total video frames decoded: {videoFramesDecoded}");
Console.WriteLine($"Total audio frames decoded: {videoFramesDecoded}");
Console.WriteLine();

Console.WriteLine("Destroying Sender & Receiver and terminating as fast as possible...");

if(sendPtr != nint.Zero)
{
    NDIWrapper.send_destroy(sendPtr);
}

NDIWrapper.recv_advertiser_destroy(advertiserPtr);
NDIWrapper.recv_destroy(receiverPtr);
Console.WriteLine("Exiting...");