# NDI to NDI High Bandwidth Converter

During a live show of NDI NOW hosted by [JB&A on LinkedIn](https://www.linkedin.com/events/7226374293364854784), someone commented that they needed a simple converter that took in an NDI HX source and transcoded it to an NDI High Bandwidth source.

Here's that utility.

Launch it, provide an NDI source name, and presto. Any incoming frames are immediately decoded and re-sent to NDI as a full bandwidth frame.

## Latency

Based on limited tests, the latency appears to be 1 frame behind the original source. This was tested with the [NDI Test Pattern Generator](https://github.com/tractusevents/NdiTestPatternGenerator) (Full Bandwidth), a BirdDog X1 (HX 3), and a Mevo Start (HX 2).

## Download

Grab the latest release on the [Releases Page](https://github.com/tractusevents/Tractus.NdiToNdiHB/releases/).
