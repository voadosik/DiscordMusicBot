# DiscordMusicBot-Lavalink4NET

Welcome to the Discord Music Bot using Lavalink4NET. This bot is designed to provide music streaming in your Discord server.

## Features

- Supports playback from various sources (SoundCloud, Spotify, etc.)
- Easy to use commands for controlling music playback
- Support for multiple servers
- Volume control

## Prerequisites

Before you start, ensure you have the following:

- JDK (version 17 or higher)
- A Discord bot token
- Discord guild ID
- A running Lavalink server

## Installation

1. **Clone the repository:**

   ```bash
   git clone https://github.com/voadosik/DiscordMusicBot-Lavalink4NET.git
   cd DiscordMusicBot-Lavalink4NET
   ```
2. **Download JDK**
   Download JDK development kit for your operating system by the following link:
   ```
   https://www.oracle.com/java/technologies/downloads/
   ```
3. **Download Lavalink**
   Download the latest stable version of LavaLink.jar by the following link:
   ```
   https://github.com/lavalink-devs/Lavalink/releases
   ```
   Upload this file to the folder with JDK.
   Create application.yml file with the following content:
   ```application.yml
       server: # REST and WS server
      port: 2333
      address: 0.0.0.0
      http2:
        enabled: false 
    plugins:
    lavalink:
      plugins:
      server:
        password: "youshallnotpass"
        sources:
          youtube: true
          bandcamp: true
          soundcloud: true
          twitch: true
          vimeo: true
          nico: true
          http: true # warning: keeping HTTP enabled without a proxy configured could expose your server's IP address.
          local: false
        filters: # All filters are enabled by default
          volume: true
          equalizer: true
          karaoke: true
          timescale: true
          tremolo: true
          vibrato: true
          distortion: true
          rotation: true
          channelMix: true
          lowPass: true
        bufferDurationMs: 400 # The duration of the NAS buffer. Higher values fare better against longer GC pauses. Duration <= 0 to disable JDA-NAS. Minimum of 40ms, lower values may introduce pauses.
        frameBufferDurationMs: 5000 # How many milliseconds of audio to keep buffered
        opusEncodingQuality: 10 # Opus encoder quality. Valid values range from 0 to 10, where 10 is best quality but is the most expensive on the CPU.
        resamplingQuality: LOW # Quality of resampling operations. Valid values are LOW, MEDIUM and HIGH, where HIGH uses the most CPU.
        trackStuckThresholdMs: 10000 # The threshold for how long a track can be stuck. A track is stuck if does not return any audio data.
        useSeekGhosting: true # Seek ghosting is the effect where whilst a seek is in progress, the audio buffer is read from until empty, or until seek is ready.
        youtubePlaylistLoadLimit: 6 # Number of pages at 100 each
        playerUpdateInterval: 5 # How frequently to send player updates to clients, in seconds
        youtubeSearchEnabled: true
        soundcloudSearchEnabled: true
        gc-warnings: true
    
    metrics:
      prometheus:
        enabled: false
        endpoint: /metrics
    
    sentry:
      dsn: ""
      environment: ""
    
    
    logging:
      file:
        path: ./logs/
    
      level:
        root: INFO
        lavalink: INFO
    
      request:
        enabled: true
        includeClientInfo: true
        includeHeaders: false
        includeQueryString: true
        includePayload: true
        maxPayloadLength: 10000
    
    
      logback:
        rollingpolicy:
          max-file-size: 1GB
          max-history: 30
   ```
4.**Launch a Lavalink server**
  In jdk/bin directory:
  ```bash
    java -jar Lavalink.jar
  ```

5. **Compile the bot**
   Compile the bot in any text editor which suits to you.
   Go to the newly created bin/Debug/net8.0 directory of the cloned project.
   Create a new appsettings.json file with the following contents:
   ```appsettings.json
   {
      "Discord": {
        "Token": "Your bot token",
        "GuildID": "Your server Guild ID"
      },
      "Lavalink": {
        "RestUri": "http://localhost:2333",
        "WebSocketUri": "ws://localhost:2333/ws",
        "Authorization": "youshallnotpass"
      }
    }

   ```
6. **Run the bot and use it**

**Usage:**
1. ```/play <url>``` - play a song from a URL.
2. ```/disconnect``` - disconnect from the voice channel.
3. ```/pause``` - pause the current song.
4. ```/resume``` - resume the paused song.
5. ```/position``` - get the current time position of the track.
6. ```/stop``` - stop the current song.
7. ```/skip``` - skips the current song.
8. ```/volume (0 - 100)``` - set the volume of player.


