using System;
using UnityEngine;

namespace Managers
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _sfxVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _musicVolume = 1f;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip _starCollect;
        [SerializeField] private AudioClip _heartCollect;
        [SerializeField] private AudioClip _extraLife;
        [SerializeField] private AudioClip _playerHurt;
        [SerializeField] private AudioClip _playerDeath;
        [SerializeField] private AudioClip _checkpointActivate;
        [SerializeField] private AudioClip _gameOver;
        [SerializeField] private AudioClip _jump;
        [SerializeField] private AudioClip _respawn;
        [SerializeField] private AudioClip _checkpointRespawn;

        [Header("Footsteps")]
        [SerializeField] private AudioClip[] _footsteps;
        [Range(0f, 1f)]
        [SerializeField] private float _footstepVolume = 0.5f;

        [Header("Music")]
        [SerializeField] private AudioClip _backgroundMusic;

        private void Start()
        {
            if (_backgroundMusic != null)
            {
                PlayMusic(_backgroundMusic);
            }
        }

        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || _sfxSource == null) return;

            float volume = _masterVolume * _sfxVolume * volumeMultiplier;
            _sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null || _musicSource == null) return;

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _masterVolume * _musicVolume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        private void UpdateMusicVolume()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _masterVolume * _musicVolume;
            }
        }

        public void PlayStarCollect() => PlaySFX(_starCollect);
        public void PlayHeartCollect() => PlaySFX(_heartCollect);
        public void PlayExtraLife() => PlaySFX(_extraLife);
        public void PlayPlayerHurt() => PlaySFX(_playerHurt);
        public void PlayPlayerDeath() => PlaySFX(_playerDeath);
        public void PlayCheckpointActivate() => PlaySFX(_checkpointActivate);
        public void PlayGameOver() => PlaySFX(_gameOver);
        public void PlayJump() => PlaySFX(_jump);
        public void PlayRespawn() => PlaySFX(_respawn);
        public void PlayCheckpointRespawn() => PlaySFX(_checkpointRespawn);

        public void PlayFootstep()
        {
            if (_footsteps == null || _footsteps.Length == 0) return;

            int index = UnityEngine.Random.Range(0, _footsteps.Length);
            PlaySFX(_footsteps[index], _footstepVolume);
        }
    }
}
