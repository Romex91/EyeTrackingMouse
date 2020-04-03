using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlindConfigurationTester
{
    static class CalibrationModesForTesting
    {
        public static eye_tracking_mouse.Options.CalibrationMode[] Short
        {
            get
            {
                return new eye_tracking_mouse.Options.CalibrationMode[]
                 {
                    //new eye_tracking_mouse.Options.CalibrationMode
                    //{
                    //    considered_zones_count = 5,
                    //    max_zones_count = 2000,
                    //    shade_thickness_in_pixels = 50,
                    //    size_of_opaque_sector_in_percents = 30,
                    //    size_of_transparent_sector_in_percents = 30,
                    //    zone_size = 150,

                    //    correction_fade_out_distance = 800,
                    //    correction_fade_out_power = 2,

                    //    algorithm = "V0",
                    //    update_period_ms = 0,
                    //    additional_dimensions_configuration =
                    //    new eye_tracking_mouse.AdditionalDimensionsConfguration
                    //    {
                    //        LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                    //        RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    //        AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    //        HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    //        HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                    //    }
                    //},
                    // new eye_tracking_mouse.Options.CalibrationMode
                    //{
                    //    considered_zones_count = 5,
                    //    max_zones_count = 2000,
                    //    shade_thickness_in_pixels = 50,
                    //    size_of_opaque_sector_in_percents = 30,
                    //    size_of_transparent_sector_in_percents = 30,
                    //    zone_size = 150,

                    //    correction_fade_out_distance = 800,
                    //    correction_fade_out_power = 2,

                    //    algorithm = "V1",
                    //    update_period_ms = 0,
                    //    additional_dimensions_configuration =
                    //    new eye_tracking_mouse.AdditionalDimensionsConfguration
                    //    {
                    //        LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                    //        RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    //        AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    //        HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    //        HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                    //    }
                    //},
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        correction_fade_out_distance = 100,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                        new eye_tracking_mouse.AdditionalDimensionsConfguration
                        {
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                        }
                    }
                 };
            }
        }
        public static eye_tracking_mouse.Options.CalibrationMode[] Long
        {
            get
            {
                return new eye_tracking_mouse.Options.CalibrationMode[]
                 {
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0  },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents {X = 700, Y = 700, Z = 700}
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                        new eye_tracking_mouse.AdditionalDimensionsConfguration
                        {
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                        }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 2000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                        new eye_tracking_mouse.AdditionalDimensionsConfguration
                        {
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                        }
                    },
                    ///// 1000/////
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 1000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 1000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 1000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 1000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0  },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents {X = 700, Y = 700, Z = 700}
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 1000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                            }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 1000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                        new eye_tracking_mouse.AdditionalDimensionsConfguration
                        {
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                        }
                    },
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 1000,
                        shade_thickness_in_pixels = 50,
                        size_of_opaque_sector_in_percents = 30,
                        size_of_transparent_sector_in_percents = 30,
                        zone_size = 150,

                        algorithm = "V2",
                        update_period_ms = 0,
                        additional_dimensions_configuration =
                        new eye_tracking_mouse.AdditionalDimensionsConfguration
                        {
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                        }
                    }
                 };
            }
        }
    }
}