using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Standard categories for fitness goals
    /// </summary>
    public enum GoalCategory
    {
        /// <summary>
        /// Goals focused on increasing strength (e.g., increase max squat weight)
        /// </summary>
        [Display(Name = "Strength")]
        Strength = 0,

        /// <summary>
        /// Goals focused on cardiovascular fitness (e.g., complete 5K run)
        /// </summary>
        [Display(Name = "Cardio")]
        Cardio = 1,

        /// <summary>
        /// Goals focused on muscle building and size (e.g., increase arm circumference)
        /// </summary>
        [Display(Name = "Hypertrophy")]
        Hypertrophy = 2,

        /// <summary>
        /// Goals focused on weight management (e.g., lose/gain weight)
        /// </summary>
        [Display(Name = "Weight")]
        Weight = 3,

        /// <summary>
        /// Goals focused on exercise technique (e.g., improve squat form)
        /// </summary>
        [Display(Name = "Technique")]
        Technique = 4,

        /// <summary>
        /// Goals focused on mobility and flexibility (e.g., touch toes)
        /// </summary>
        [Display(Name = "Mobility")]
        Mobility = 5,

        /// <summary>
        /// Goals focused on endurance (e.g., run for 60 minutes without stopping)
        /// </summary>
        [Display(Name = "Endurance")]
        Endurance = 6,

        /// <summary>
        /// Goals focused on athletic performance (e.g., improve vertical jump)
        /// </summary>
        [Display(Name = "Performance")]
        Performance = 7,

        /// <summary>
        /// Goals focused on overall wellness and health
        /// </summary>
        [Display(Name = "Wellness")]
        Wellness = 8,

        /// <summary>
        /// Goals that don't fit any of the standard categories
        /// </summary>
        [Display(Name = "Other")]
        Other = 9
    }
}