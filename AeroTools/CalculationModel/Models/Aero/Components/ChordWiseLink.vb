﻿'Open VOGEL (www.openvogel.com)
'Open source software for aerodynamics
'Copyright (C) 2016 Guillermo Hazebrouck (guillermo.hazebrouck@openvogel.com)

'This program Is free software: you can redistribute it And/Or modify
'it under the terms Of the GNU General Public License As published by
'the Free Software Foundation, either version 3 Of the License, Or
'(at your option) any later version.

'This program Is distributed In the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty Of
'MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License For more details.

'You should have received a copy Of the GNU General Public License
'along with this program.  If Not, see < http:  //www.gnu.org/licenses/>.

Imports System.IO
Imports AeroTools.DataStacks
Imports MathTools.Algebra.EuclideanSpace

Namespace CalculationModel.Models.Aero.Components

    ''' <summary>
    ''' Represents a vortex ring stripe where lift and drag can be locally computed
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ChorwiseStripe

        ''' <summary>
        ''' Chordwise stripe of vortex rings (from the L.E. to the T.E)
        ''' </summary>
        ''' <remarks></remarks>
        Public Rings As List(Of VortexRing)

        ''' <summary>
        ''' Polar curve used to compute the local drag
        ''' </summary>
        ''' <remarks></remarks>
        Public Polars As PolarFamily

        Public Sub New()
            Rings = New List(Of VortexRing)
        End Sub

        Private _Area As Double

        Public ReadOnly Property Area As Double
            Get
                Return _Area
            End Get
        End Property

        Private _CL As Double

        ''' <summary>
        ''' Stripe lift coefficient
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property CL As Double
            Get
                Return _CL
            End Get
        End Property

        Private _CDi As Double

        ''' <summary>
        ''' Stripe induced drag coefficient
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property CDi As Double
            Get
                Return _CDi
            End Get
        End Property

        Private _CDp As Double

        ''' <summary>
        ''' Stripe parasitic drag coefficient
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property CDp As Double
            Get
                Return _CDp
            End Get
        End Property

        Public _L As EVector3

        ''' <summary>
        ''' Total stripe lift
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property L As EVector3
            Get
                Return _L
            End Get
        End Property

        Public _Di As EVector3

        ''' <summary>
        ''' Total stripe induced drag
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property Di As EVector3
            Get
                Return _Di
            End Get
        End Property

        Public _Dp As EVector3

        ''' <summary>
        ''' Total stripe induced drag
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property Dp As EVector3
            Get
                Return _Dp
            End Get
        End Property

        Public _ML As EVector3

        ''' <summary>
        ''' Total stripe moment (with respect to the origin).
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property ML As EVector3
            Get
                Return _ML
            End Get
        End Property

        Public _MDi As EVector3

        ''' <summary>
        ''' Total stripe moment (with respect to the origin).
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property MDi As EVector3
            Get
                Return _MDi
            End Get
        End Property

        Public _MDp As EVector3

        ''' <summary>
        ''' Total stripe moment (with respect to the origin).
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property MDp As EVector3
            Get
                Return _MDp
            End Get
        End Property

        Private _Chord As Double

        ''' <summary>
        ''' Stripe chord
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Chord As Double
            Get
                Return _Chord
            End Get
        End Property

        Private _ChordWiseVector As New EVector3

        ''' <summary>
        ''' Vector having the direction of the chord
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ChordWiseVector
            Get
                Return _ChordWiseVector
            End Get
        End Property

        Private _CenterPoint As New EVector3

        ''' <summary>
        ''' Point located at the geometric center of the chordwise stripe
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property CenterPoint As EVector3
            Get
                Return _CenterPoint
            End Get
        End Property

        ''' <summary>
        ''' Calculate stripe lift, drag and area. Cp should be calculated before calling this sub.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub Compute(ByVal StreamDirection As EVector3, ByVal V As Double, Rho As Double, Mu As Double) ' the stream direction should be an argument...

            ' Calculate local chordwise direction and chord:

            Dim n As Integer = Rings.Count - 1

            _ChordWiseVector.X = 0.5 * ((Rings(n).Node(1).Position.X - Rings(0).Node(2).Position.X) + (Rings(n).Node(4).Position.X - Rings(0).Node(3).Position.X))
            _ChordWiseVector.Y = 0.5 * ((Rings(n).Node(1).Position.Y - Rings(0).Node(2).Position.Y) + (Rings(n).Node(4).Position.Y - Rings(0).Node(3).Position.Y))
            _ChordWiseVector.Z = 0.5 * ((Rings(n).Node(1).Position.Z - Rings(0).Node(2).Position.Z) + (Rings(n).Node(4).Position.Z - Rings(0).Node(3).Position.Z))

            _CenterPoint.X = 0.5 * (Rings(0).Node(1).Position.X + Rings(0).Node(4).Position.X) + _ChordWiseVector.X
            _CenterPoint.Y = 0.5 * (Rings(0).Node(1).Position.Y + Rings(0).Node(4).Position.Y) + _ChordWiseVector.Y
            _CenterPoint.Z = 0.5 * (Rings(0).Node(1).Position.Z + Rings(0).Node(4).Position.Z) + _ChordWiseVector.Z

            _Chord = _ChordWiseVector.EuclideanNorm
            _ChordWiseVector.Normalize()

            _CL = 0.0#
            _Area = 0.0#
            _CDp = 0
            _CDi = 0

            Dim Projection As Double
            Dim Force As Double
            Dim InducedDrag As Double = 0

            Dim LocalL = New EVector3
            Dim LocalDi = New EVector3
            Dim LocalML As New EVector3
            Dim LocalMDi As New EVector3
            Dim LocalMDp As New EVector3

            _L = New EVector3
            _Di = New EVector3
            _Dp = New EVector3
            _ML = New EVector3
            _MDi = New EVector3
            _MDp = New EVector3

            ' Sum contributions to CL and CDi:

            For Each VortexRing In Rings

                _Area += VortexRing.Area
                InducedDrag += VortexRing.Cdi * VortexRing.Area

                Force = VortexRing.Cp * VortexRing.Area
                Projection = VortexRing.Normal.InnerProduct(StreamDirection)

                LocalL.X = Force * (VortexRing.Normal.X - Projection * StreamDirection.X)
                LocalL.Y = Force * (VortexRing.Normal.Y - Projection * StreamDirection.Y)
                LocalL.Z = Force * (VortexRing.Normal.Z - Projection * StreamDirection.Z)

                LocalDi.X = VortexRing.Cdi * VortexRing.Area * StreamDirection.X
                LocalDi.Y = VortexRing.Cdi * VortexRing.Area * StreamDirection.Y
                LocalDi.Z = VortexRing.Cdi * VortexRing.Area * StreamDirection.Z

                LocalML.FromVectorProduct(VortexRing.ControlPoint, LocalL)
                LocalMDi.FromVectorProduct(VortexRing.ControlPoint, LocalDi)

                _ML.Add(LocalML)
                _MDi.Add(LocalMDi)

                _L.X += LocalL.X
                _L.Y += LocalL.Y
                _L.Z += LocalL.Z

                _Di.X += LocalDi.X
                _Di.Y += LocalDi.Y
                _Di.Z += LocalDi.Z

            Next

            _L.X /= _Area
            _L.Y /= _Area
            _L.Z /= _Area

            _Di.X /= _Area
            _Di.Y /= _Area
            _Di.Z /= _Area

            _ML.X /= _Area
            _ML.Y /= _Area
            _ML.Z /= _Area

            _MDi.X /= _Area
            _MDi.Y /= _Area
            _MDi.Z /= _Area

            _MDp.X /= _Area
            _MDp.Y /= _Area
            _MDp.Z /= _Area

            _CL = _L.EuclideanNorm
            _CDi = InducedDrag / _Area

            ' Calculate _CDp from CL (if there is a polar curve):

            Dim Re As Double = V * Rho * Chord / Mu

            If Not IsNothing(Polars) Then
                _CDp = Polars.SkinDrag(CL, Re)
            End If

            _Dp.X = _CDp * ChordWiseVector.X
            _Dp.Y = _CDp * ChordWiseVector.Y
            _Dp.Z = _CDp * ChordWiseVector.Z

            LocalMDp.FromVectorProduct(CenterPoint, Dp)
            _MDp.Add(LocalMDi)

        End Sub

        Sub ReadBinary(ByRef r As BinaryReader, ByRef Rings As List(Of VortexRing), ByRef PolarDB As PolarDatabase)
            Try
                For i = 1 To r.ReadInt32
                    Me.Rings.Add(Rings(r.ReadInt32))
                Next
                Dim polarID = New Guid(r.ReadString())
                Polars = PolarDB.GetFamilyFromID(polarID)
            Catch ex As Exception
                Me.Rings.Clear()
            End Try
        End Sub

        Sub WriteBinary(ByRef w As BinaryWriter)

            w.Write(Rings.Count)

            For Each Ring In Rings
                w.Write(Ring.IndexL)
            Next

            If IsNothing(Polars) Then
                w.Write(Guid.Empty.ToString)
            Else
                w.Write(Polars.ID.ToString)
            End If

        End Sub

    End Class

End Namespace