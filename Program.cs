using NewTek;
using NewTek.NDI;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Tractus.NdiToNdiHB;

internal class Program
{
    static unsafe void Main(string[] args)
    {
        var sourceName = args.Length == 1 ? args[0] : string.Empty;

        Console.WriteLine("Tractus HX to HB for NDI Converter");
        Console.WriteLine("Find more of our tools at https://www.tractusevents.com/tools");

        while (string.IsNullOrEmpty(sourceName)) 
        {
            Console.WriteLine("Please provide an NDI source name. Source names are CaSe SeNsItIvE!");
            sourceName = Console.ReadLine();
        }

        var destinationName = $"{sourceName.Replace("(", "").Replace(")", "")} (ToHB)";

        var inboundVideoFrame = new NDIlib.video_frame_v2_t();
        var inboundAudioFrame = new NDIlib.audio_frame_v3_t();

        var createSettings = new NDIlib.recv_create_v3_t
        {
            allow_video_fields = true,
            bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
            color_format = NDIlib.recv_color_format_e.recv_color_format_fastest,
            p_ndi_recv_name = UTF.StringToUtf8("TractusNdiToNdi"),
            source_to_connect_to = new NDIlib.source_t
            {
                p_ndi_name = UTF.StringToUtf8(sourceName),
            }
        };

        var receiverPtr = NDIlib.recv_create_v3(ref createSettings);

        var sendSettings = new NDIlib.send_create_t
        {
            p_ndi_name = UTF.StringToUtf8(destinationName)
        };

        var sendPtr = NDIlib.send_create(ref sendSettings);

        Marshal.FreeHGlobal(sendSettings.p_ndi_name);
        Marshal.FreeHGlobal(createSettings.p_ndi_recv_name);
        Marshal.FreeHGlobal(createSettings.source_to_connect_to.p_ndi_name);

        var running = true;

        Console.CancelKeyPress += (o, e) =>
        {
            running = false;
            e.Cancel = true;
            Console.WriteLine("Received Ctrl+C. Exiting...");
        };

        Console.WriteLine($"Broadcasting '{sourceName} as {destinationName}'. Press Ctrl+C to exit.");
        var videoFramesDecoded = 0;
        var audioFramesDecoded = 0;
        var noFramesDecoded = true;

        while (running)
        {
            var frameType = NDIlib.recv_capture_v3(
                receiverPtr, 
                &inboundVideoFrame, 
                &inboundAudioFrame, 
                null, 
                1000);

            if(frameType == NDIlib.frame_type_e.frame_type_none)
            {
                Console.WriteLine("We didn't get any frames. Waiting another second...");
                continue;
            }

            if (noFramesDecoded)
            {
                Console.WriteLine("Now receiving frames!");
                noFramesDecoded = false;
            }

            if(frameType == NDIlib.frame_type_e.frame_type_video)
            {
                NDIlib.send_send_video_v2(sendPtr, ref inboundVideoFrame);
                NDIlib.recv_free_video_v2(receiverPtr, ref inboundVideoFrame);
                videoFramesDecoded++;
            }

            if(frameType == NDIlib.frame_type_e.frame_type_audio)
            {
                NDIlib.send_send_audio_v3(sendPtr, ref inboundAudioFrame);
                NDIlib.recv_free_audio_v3(receiverPtr, ref inboundAudioFrame);
                audioFramesDecoded++;
            }
        }

        Console.WriteLine($"Total video frames decoded: {videoFramesDecoded}");
        Console.WriteLine($"Total audio frames decoded: {videoFramesDecoded}");
        Console.WriteLine();

        Console.WriteLine("Destroying Sender & Receiver and terminating as fast as possible...");
        NDIlib.send_destroy(sendPtr);
        NDIlib.recv_destroy(receiverPtr);
        Console.WriteLine("Exiting...");
    }
}
