open System
open Deedle
open System.IO
open Plotly.NET
open Plotly.NET.LayoutObjects
open Plotly.NET.TraceObjects
open Plotly.NET.ConfigObjects
open DynamicObj
open Giraffe.ViewEngine
open Newtonsoft.Json

[<EntryPoint>]
let main argv =
    [

        Chart.Point(
            x = [0 .. 10],
            y = [0 .. 10],
            UseDefaults = false, 
            ShowLegend = true
        )
        |> Chart.withXAxis(
            LinearAxis.init(
                AutoRangeOptions = AutoRangeOptions.init(
                    MinAllowed = 4,
                    ClipMax = 8
                )
            )
        )
        |> Chart.withYAxis(
            LinearAxis.init(
                AutoRangeOptions = AutoRangeOptions.init(
                    MinAllowed = 4,
                    ClipMax = 8
                )
            )
        )


        Chart.Point(
            x = [0 .. 10],
            y = [0 .. 10],
            UseDefaults = false, 
            ShowLegend = true
        )
        |> Chart.withXAxis(
            LinearAxis.init(
                MinAllowed = 4,
                MaxAllowed = 8
            )
        )
        |> Chart.withYAxis(
            LinearAxis.init(
                MinAllowed = 4,
                MaxAllowed = 8
            )
        )



        Chart.Point3D(
            x = [0 .. 10],
            y = [0 .. 10],
            z = [0 .. 10],
            UseDefaults = false, 
            ShowLegend = true,
            CameraProjectionType = StyleParam.CameraProjectionType.Orthographic
        )
        |> Chart.withXAxis(
            LinearAxis.init(
                AutoRangeOptions = AutoRangeOptions.init(
                    MinAllowed = 4,
                    ClipMax = 8
                )
            ),
            Id = StyleParam.SubPlotId.Scene 1
        )
        |> Chart.withYAxis(
            LinearAxis.init(
                AutoRangeOptions = AutoRangeOptions.init(
                    MinAllowed = 4,
                    ClipMax = 8
                )
            ),
            Id = StyleParam.SubPlotId.Scene 1
        )
        |> Chart.withZAxis(
            LinearAxis.init(
                AutoRangeOptions = AutoRangeOptions.init(
                    MinAllowed = 4,
                    ClipMax = 8
                )
            )
        )



        Chart.Point3D(
            x = [0 .. 10],
            y = [0 .. 10],
            z = [0 .. 10],
            UseDefaults = false, 
            ShowLegend = true,
            CameraProjectionType = StyleParam.CameraProjectionType.Orthographic
        )
        |> Chart.withXAxis(
            LinearAxis.init(
                MinAllowed = 4,
                MaxAllowed = 8
            ),
            Id = StyleParam.SubPlotId.Scene 1
        )
        |> Chart.withYAxis(
            LinearAxis.init(
                MinAllowed = 4,
                MaxAllowed = 8
            ),
            Id = StyleParam.SubPlotId.Scene 1
        )
        |> Chart.withZAxis(
            LinearAxis.init(
                MinAllowed = 4,
                MaxAllowed = 8
            )
        )



        Chart.PointPolar(
            r = [0 .. 10],
            theta = [0 .. 10 .. 100],
            UseDefaults = false, 
            ShowLegend = true
        )
        |> Chart.withRadialAxis(
            RadialAxis.init(
                AutoRangeOptions = AutoRangeOptions.init(
                    MinAllowed = 4,
                    MaxAllowed = 8
                )
            )
        )


    
        Chart.PointPolar(
            r = [0 .. 10],
            theta = [0 .. 10 .. 100],
            UseDefaults = false, 
            ShowLegend = true
        )
        |> Chart.withRadialAxis(
            RadialAxis.init(
                MinAllowed = 4,
                MaxAllowed = 8
            )
        )
    ]
    |> Seq.iter Chart.show
    0