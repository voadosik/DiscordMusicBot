using Discord.Interactions;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET;

namespace Core.AudioHandler
{

  //Handler of all the commands in a Discord guild context 

  [RequireContext(ContextType.Guild)]
  public sealed class AudioHandler : InteractionModuleBase<SocketInteractionContext>
  {

    private readonly IAudioService _audioService;

    //Audio Service initializer
    public AudioHandler(IAudioService audioService)
    {
      ArgumentNullException.ThrowIfNull(audioService);
      _audioService = audioService;
    }



    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task Play(string query, [Summary("source", "The source to search (youtube, soundcloud, spotify)")] string source = "youtube")
    {
      // Defers the response to acknowledge the command
      await DeferAsync().ConfigureAwait(false);
      var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);
      if (player is null) return;

     
      // Determine the track search mode based on the provided source
      TrackSearchMode searchMode = source.ToLower() switch
      {  
        "soundcloud" => TrackSearchMode.SoundCloud,
        "spotify" => TrackSearchMode.Spotify,
        _ => TrackSearchMode.YouTube
      };

      // Load the track from the specified streaming service
      var track = await _audioService.Tracks
          .LoadTrackAsync(query, searchMode)
          .ConfigureAwait(false);

      if (track is null)
      {
        await FollowupAsync("No results.").ConfigureAwait(false);
        return;
      }
      // Play the track
      var position = await player.PlayAsync(track).ConfigureAwait(false);

      // Send appropriate follow-up message based on whether the track is playing or added to the queue
      if (position is 0) await FollowupAsync($"Playing: {track.Uri}").ConfigureAwait(false);
      else await FollowupAsync($"Added to queue: {track.Uri}").ConfigureAwait(false);
      
    }

    [SlashCommand("position", description: "Shows the track position", runMode: RunMode.Async)]
    public async Task Position()
    {
      var player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(false);

      if (player is null) return;
      

      if (player.CurrentItem is null)
      {
        await RespondAsync("Nothing playing!").ConfigureAwait(false);
        return;
      }

      // Respond with the current track position and duration
      await RespondAsync(
              $"Position: {player.Position?.Position.Minutes}:{player.Position?.Position.Seconds:00} / {player.CurrentTrack.Duration.Minutes:00}:{player.CurrentTrack.Duration.Seconds:00}.")
          .ConfigureAwait(false);
    }

    [SlashCommand("stop", description: "Stops the current track", runMode: RunMode.Async)]
    public async Task Stop()
    {
      var player = await GetPlayerAsync(connectToVoiceChannel: false);

      if (player is null) return;
      

      if (player.CurrentItem is null)
      {
        await RespondAsync("Nothing playing!").ConfigureAwait(false);
        return;
      }

      // Stop the player and send message to the chat
      await player.StopAsync().ConfigureAwait(false);
      await RespondAsync("Stopped playing.").ConfigureAwait(false);
    }

    [SlashCommand("disconnect", "Disconnects from the current voice channel connected to", runMode: RunMode.Async)]
    public async Task Disconnect()
    {
      var player = await GetPlayerAsync().ConfigureAwait(false);
      if (player is null) return;

      // Disconnect the player and send message to the chat
      await player.DisconnectAsync().ConfigureAwait(false);
      await RespondAsync("Disconnected.").ConfigureAwait(false);
    }

    [SlashCommand("volume", description: "Sets the player volume, possible values are: 0 - 100", runMode: RunMode.Async)]
    public async Task Volume(int volume = 50)
    {

      // Check if the volume is within the valid range
      if (volume is > 100 or < 0)
      {
        await RespondAsync("Volume out of range: 0% - 100%!").ConfigureAwait(false);
        return;
      }

      var player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(false);

      if (player is null) return;

      // Set the player volume
      await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
      await RespondAsync($"Volume updated: {volume}%").ConfigureAwait(false);
    }

    [SlashCommand("skip", description: "Skips the current track", runMode: RunMode.Async)]
    public async Task Skip()
    {
      var player = await GetPlayerAsync(connectToVoiceChannel: false);

      if (player is null) return;
      
      if (player.CurrentItem is null)
      {
        await RespondAsync("Nothing is currently playing").ConfigureAwait(false);
        return;
      }

      // Skip the current track
      await player.SkipAsync().ConfigureAwait(false);
      var track = player.CurrentItem;

      // Respond with appropriate message based on the next track in the queue
      if (track is not null) await RespondAsync($"Skipped. Now playing: {track.Track!.Uri}").ConfigureAwait(false);
      else await RespondAsync("Skipped. Stopped playing because the queue is now empty.").ConfigureAwait(false);
      
    }

    [SlashCommand("pause", description: "Pauses the player.", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
      var player = await GetPlayerAsync(connectToVoiceChannel: false);

      if (player is null) return;
      
      if (player.State is PlayerState.Paused)
      {
        await RespondAsync("Player is already paused.").ConfigureAwait(false);
        return;
      }
      // Pause the player
      await player.PauseAsync().ConfigureAwait(false);
      await RespondAsync("Paused.").ConfigureAwait(false);
    }

    [SlashCommand("resume", description: "Resumes the player.", runMode: RunMode.Async)]
    public async Task ResumeAsync()
    {
      var player = await GetPlayerAsync(connectToVoiceChannel: false);
      if (player is null) return;
      
      if (player.State is not PlayerState.Paused)
      {
        await RespondAsync("Player is not paused.").ConfigureAwait(false);
        return;
      }
      // Resume the player 
      await player.ResumeAsync().ConfigureAwait(false);
      await RespondAsync("Resumed.").ConfigureAwait(false);
    }

    // Helper method to retrieve the player and optionally connect to a voice channel
    private async ValueTask<VoteLavalinkPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
    {
      var retrieveOptions = new PlayerRetrieveOptions(
          ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

      var result = await _audioService.Players
          .RetrieveAsync(Context, playerFactory: PlayerFactory.Vote, retrieveOptions)
          .ConfigureAwait(false);

      if (!result.IsSuccess)
      {
        // Determine the error message based on the retrieval status
        var errorMessage = result.Status switch
        {
          PlayerRetrieveStatus.UserNotInVoiceChannel => "Firstly, connect to vc.",
          PlayerRetrieveStatus.BotNotConnected => "Bot is not in vc.",
          _ => "Unknown error.",
        };
        await FollowupAsync(errorMessage).ConfigureAwait(false);
        return default;
      }
      return result.Player;
    }
  }
}