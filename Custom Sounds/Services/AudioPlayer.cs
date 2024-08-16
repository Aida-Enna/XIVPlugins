using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CustomSounds.Utility;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CustomSounds.Services;

public class AudioPlayer : IDisposable
{
    public static AudioPlayer Instance { get; } = new();

    private readonly IWavePlayer _outputDevice;
    private readonly VolumeSampleProvider _sampleProvider;
    private readonly MixingSampleProvider _mixer;

    //private WaveOutEvent outputDevice = new WaveOutEvent();

    /// <summary>
    /// Service used for playing audio files from the Resources folder.
    /// Audio mixer implementation referenced from https://github.com/Roselyyn/EldenRingDalamud
    /// </summary>
    public AudioPlayer()
    {
        _outputDevice = new WaveOutEvent();
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };
        _sampleProvider = new VolumeSampleProvider(_mixer);


        _outputDevice.Init(_sampleProvider);
        _outputDevice.Play();

        UpdateVolume();
    }

    public void Dispose()
    {
        _outputDevice.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Updates the volume of the output mixer.
    /// </summary>
    public void UpdateVolume()
    {
        float targetVolume = 0.5f;

        try
        {
            // Updates the volume using the FFXIV configured volume.
            // Takes the configured volume source and multiplies it by the master volume amount.
            // Additionally adds volume boost before applying the master volume multiplier.
            if (Configuration.Instance.BindToXivVolumeSource)
            {
                uint baseVolume = XivUtility.GetVolume(Configuration.Instance.XivVolumeSource);
                uint masterVolume = XivUtility.GetVolume(XivVolumeSource.Master);
                uint baseVolumeBoost = Configuration.Instance.XivVolumeSourceBoost;

                if (baseVolume == 0) targetVolume = 0;
                else targetVolume = Math.Clamp((baseVolume + baseVolumeBoost) * (masterVolume / 100f), 0, 100) / 100f;
            }
            // Updates the volume from the configured volume amount.
            else
            {
                targetVolume = Math.Clamp(Configuration.Instance.Volume, 0, 100) / 100f;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Exception was thrown while setting volume");
        }
        finally
        {
            Plugin.Log.Debug("Setting volume to {TargetVolume}", targetVolume);
            _sampleProvider.Volume = targetVolume;
        }
    }

    /// <summary>
    /// Plays a sound from the supplied path in the Resources directory.
    /// </summary>
    /// <param name="resourcePath">The path of the file to play.</param>
    public void PlaySoundSimple(string resourcePath)
    {
        string audioPath = GetFilepathForResource(resourcePath);
        var audioFile = new AudioFileReader(audioPath);
        //WaveOutEvent outputDevice = new WaveOutEvent();
        _mixer.AddMixerInput(ConvertToCorrectChannelCount(audioFile));
        //outputDevice.Init(audioFile);
        //outputDevice.Play();
    }

    private ISampleProvider ConvertToCorrectChannelCount(ISampleProvider input)
    {
        int inputChannels = input.WaveFormat.Channels;
        int mixerChannels = _mixer.WaveFormat.Channels;

        if (inputChannels == mixerChannels) return input;
        if (inputChannels == 1 && mixerChannels == 2)
        {
            return new MonoToStereoSampleProvider(input);
        }

        throw new NotImplementedException($"Conversion from {inputChannels} to {mixerChannels} channels is not yet implemented");
    }

    private static string GetFilepathForResource(string resourcePath)
    {
        string assetsDir = AssetsManager.GetAssetsDirectory();
        string baseAudioPath = $"{assetsDir}/{resourcePath}";

        return Configuration.Instance.AssetsFileType switch
        {
            AssetsFileType.Mp3 => $"{baseAudioPath}.mp3",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}