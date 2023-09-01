using Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility
{
    public class Timer : MonoBehaviour
	{
		public static Timer Instance;

		private List<TimerData> _ongoingPausableTimers = new List<TimerData>();
		private List<TimerData> _ongoingTimers = new List<TimerData>();

		private PlayerInfoManager _playerInfoManager;

		public class TimerData
		{
			public Guid ID;
			public Action OnComplete;
			public float TimeLeft;

			public TimerData(Guid guid, Action onComplete, float timeLeft)
			{
				ID = guid;
				OnComplete = onComplete;
				TimeLeft = timeLeft;
			}
		}

		private void Awake()
		{
			if(Instance)
            {
				Destroy(this);
				return;
            }
			Instance = this;
			_playerInfoManager = PlayerInfoManager.Instance;
		}

		/// <summary>
		/// Updates the timers if the skater is not null or disabled.
		/// </summary>
		private void Update()
		{
			UpdateTimers();
			if (_playerInfoManager == null || _playerInfoManager.GamePaused)
			{
				return;
			}

			UpdatePausableTimers();
		}

		#region Public Methods
		/// <summary>
		/// Starts a new W_Timer with the given duration and onComplete Action.
		/// </summary>
		/// <param name="duration">After what time should the onComplete be invoked.</param>
		/// <param name="onComplete">What should the Timer Invoke when ready.</param>
		/// <param name="pausable">Can the timer be paused</param>
		/// <returns></returns>
		public Guid StartNewTimer(float duration, Action onComplete, bool pausable = true)
		{
			Guid guid = Guid.NewGuid();
			if (pausable)
			{
				_ongoingPausableTimers.Add(new TimerData(guid, onComplete, duration));
			}
			else
			{
				_ongoingTimers.Add(new TimerData(guid, onComplete, duration));
			}
			return guid;
		}

		/// <summary>
		/// Restart the timer and change its duration.
		/// </summary>
		/// <param name="guid">Guid of the timer you want to restart.</param>
		/// <param name="duration">Duration the timer should last.</param>
		public void RestartTimer(Guid guid, float duration)
		{
			TimerData timer = null;
			timer = _ongoingTimers.Find(t => t.ID.Equals(guid));
			if (timer == null)
			{
				timer = _ongoingPausableTimers.Find(t => t.ID.Equals(guid));
			}
			if (timer == null)
			{
				return;
			}
			timer.TimeLeft = duration;
		}

		/// <summary>
		/// Abort timer that matches the given Guid.
		/// </summary>
		/// <param name="guid">Guid of the timer you want to abort.</param>
		public void AbortTimer(Guid guid)
		{
			_ongoingPausableTimers.RemoveAll(t => t.ID.Equals(guid));
		}

		/// <summary>
		/// Get timer that belongs to the given GUID.
		/// </summary>
		/// <param name="guid">Guid of the timer needed.</param>
		/// <returns>TimerData of the timer that corresponds to the given Guid.</returns>
		public TimerData GetTimer(Guid guid)
		{
			TimerData timer = _ongoingPausableTimers.Find(t => t.ID.Equals(guid)) ?? _ongoingTimers.Find(t => t.ID.Equals(guid));
			return timer;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the given timeLeft float and returns if 0 has been reached.
		/// </summary>
		/// <param name="timeLeft">Time left to update.</param>
		/// <returns>True if timeLeft has reached 0.</returns>
		private bool UpdateTimer(ref float timeLeft)
		{
			timeLeft -= Time.deltaTime;
			if (timeLeft <= 0)
			{
				timeLeft = 0;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Update All W_Timers.
		/// </summary>
		private void UpdateTimers()
		{
			List<TimerData> wTimers = new(_ongoingTimers);
			if (wTimers.Count > 0)
			{
				foreach (TimerData t in wTimers)
				{
					if (UpdateTimer(ref t.TimeLeft))
					{
						if (t.OnComplete.Target != null)
						{
							t.OnComplete.Invoke();
						}
						_ongoingTimers.Remove(t);
					}
				}
			}
		}

		/// <summary>
		/// Update Pausable All W_Timers.
		/// </summary>
		private void UpdatePausableTimers()
		{
			List<TimerData> wTimers = _ongoingPausableTimers.ToList();
			if (wTimers.Count > 0)
			{
				foreach (TimerData t in wTimers)
				{
					if (UpdateTimer(ref t.TimeLeft))
					{
						if (t.OnComplete.Target != null)
						{
							t.OnComplete.Invoke();
						}
						_ongoingPausableTimers.Remove(t);
					}
				}
			}
		}
		#endregion
	}
}