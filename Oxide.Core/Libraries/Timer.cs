﻿using System;
using System.Collections.Generic;
using System.Threading;

using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// The timer library
    /// </summary>
    public class Timer : Library
    {
        private readonly Thread mainThread = Thread.CurrentThread;

        /// <summary>
        /// Represents a single timer instance
        /// </summary>
        public class TimerInstance
        {
            /// <summary>
            /// Gets the number of repetitions left on this timer
            /// </summary>
            public int Repetitions { get; private set; }

            /// <summary>
            /// Gets the delay between each repetition
            /// </summary>
            public float Delay { get; private set; }

            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action Callback { get; }

            /// <summary>
            /// Gets if this timer has been destroyed
            /// </summary>
            public bool Destroyed { get; private set; }

            /// <summary>
            /// Gets the plugin to which this timer belongs, if any
            /// </summary>
            public Plugin Owner { get; }

            // The next rep time
            internal float nextrep;

            /// <summary>
            /// Initializes a new instance of the TimerInstance class
            /// </summary>
            /// <param name="repetitions"></param>
            /// <param name="delay"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public TimerInstance(int repetitions, float delay, Action callback, Plugin owner)
            {
                Repetitions = repetitions;
                Delay = delay;
                Callback = callback;
                nextrep = Interface.Oxide.Now + delay;
                Owner = owner;
                if (owner != null) owner.OnRemovedFromManager += owner_OnRemovedFromManager;
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager) => Destroy();

            /// <summary>
            /// Destroys this timer
            /// </summary>
            public void Destroy()
            {
                Destroyed = true;
                if (Owner != null) Owner.OnRemovedFromManager -= owner_OnRemovedFromManager;
            }

            /// <summary>
            /// Updates this timer
            /// </summary>
            public void Update()
            {
                if (Destroyed) return;

                nextrep += Delay;

                Owner?.TrackStart();
                try
                {
                    Callback();
                }
                catch (Exception ex)
                {
                    Destroy();
                    var error_message = $"Failed to run a {Delay:0.00} timer";
                    if (Owner && Owner != null) error_message += $" in '{Owner.Name} v{Owner.Version}'";
                    Interface.Oxide.LogException(error_message, ex);
                }
                Owner?.TrackEnd();

                if (Repetitions > 0)
                {
                    Repetitions--;
                    if (Repetitions == 0) Destroy();
                }
            }
        }

        public override bool IsGlobal => false;

        private const float updateInterval = .025f;
        private float lastUpdateAt;

        private readonly List<TimerInstance> timers = new List<TimerInstance>();
        private readonly List<TimerInstance> expiredTimers = new List<TimerInstance>();

        /// <summary>
        /// Updates all timers - called every server frame
        /// </summary>
        public void Update(float delta)
        {
            var now = Interface.Oxide.Now;

            if (now < lastUpdateAt)
            {
                var difference = lastUpdateAt - now - delta;
                Interface.Oxide.LogWarning("Time travelling detected! Timers were updated {0:0.00} seconds in the future? We will attempt to recover but this should really never happen!", difference);
                foreach (var timer in timers) timer.nextrep -= difference;
                lastUpdateAt = now;
            }

            if (now < lastUpdateAt + updateInterval) return;

            lastUpdateAt = now;

            if (timers.Count < 1) return;

            for (var i = timers.Count - 1; i >= 0; i--) if (timers[i].Destroyed) timers.RemoveAt(i);

            var count = timers.Count;
            for (var i = 0; i < count; i++)
            {
                var timer = timers[i];
                if (timer != null && timer.nextrep > now) break;
                expiredTimers.Add(timer);
            }
            if (expiredTimers.Count <= 0) return;
            timers.RemoveRange(0, expiredTimers.Count);
            foreach (var timer in expiredTimers)
            {
                if (timer == null)
                {
                    Interface.Oxide.LogWarning($"A null timer instance was removed from the timer queue!");
                    continue;
                }
                timer.Update();
                // Add the timer back to the queue if it needs to fire again
                if (!timer.Destroyed) InsertTimer(timer);
            }
            expiredTimers.Clear();
        }

        private TimerInstance AddTimer(int repetitions, float delay, Action callback, Plugin owner = null)
        {
            var timer = new TimerInstance(repetitions, delay, callback, owner);
            if (Thread.CurrentThread == mainThread)
                InsertTimer(timer);
            else
                Interface.Oxide.NextTick(() => InsertTimer(timer));
            return timer;
        }

        private void InsertTimer(TimerInstance timer)
        {
            var index = timers.Count;
            for (var i = 0; i < timers.Count; i++)
            {
                if (timers[i].nextrep <= timer.nextrep) continue;
                index = i;
                break;
            }
            timers.Insert(index, timer);
        }

        /// <summary>
        /// Creates a timer that fires once
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("Once")]
        public TimerInstance Once(float delay, Action callback, Plugin owner = null) => AddTimer(1, delay, callback, owner);

        /// <summary>
        /// Creates a timer that fires many times
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="reps"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("Repeat")]
        public TimerInstance Repeat(float delay, int reps, Action callback, Plugin owner = null) => AddTimer(reps, delay, callback, owner);

        /// <summary>
        /// Creates a timer that fires once next frame
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        [LibraryFunction("NextFrame")]
        public TimerInstance NextFrame(Action callback) => AddTimer(1, 0.0f, callback);
    }
}
