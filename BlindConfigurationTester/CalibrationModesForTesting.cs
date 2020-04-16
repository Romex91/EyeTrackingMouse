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
                    new eye_tracking_mouse.Options.CalibrationMode
                    {
                        zone_size= 150,
                        max_zones_count= 2048,
                        considered_zones_count = 5,
                        update_period_ms= 0,
                        size_of_opaque_sector_in_percents= 30,
                        size_of_transparent_sector_in_percents= 60,
                        shade_thickness_in_pixels= 50,
                        correction_fade_out_distance= 50,
                        tag_for_testing= "mid_incremental",
                        algorithm= "V2",
                        additional_dimensions_configuration =
                            new eye_tracking_mouse.AdditionalDimensionsConfguration
                            {
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
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
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 0 },
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
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 0 },
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
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
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
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents {X = 600, Y = 600, Z = 600}
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
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
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
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 }
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
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 }
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
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 0 },
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
                                RightEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 0 },
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
                                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 0 },
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
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
                                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                                HeadDirection = new eye_tracking_mouse.Vector3Percents {X = 600, Y = 600, Z = 600}
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
                                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
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
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 }
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
                            RightEye = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 },
                            LeftEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                            HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 600, Y = 600, Z = 600 }
                        }
                    }
                 };
            }
        }
    }
}