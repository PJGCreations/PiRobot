
Imports System.Collections.Generic
Imports Windows.Devices.HumanInterfaceDevice

Public Class XboxHidController
    ''' <summary>
    ''' Tolerance to ignore around (0,0) for thumbstick movement
    ''' </summary>
    Const DeadzoneTolerance As Double = 4000

    ''' <summary>
    ''' Direction the controller is indicating.
    ''' </summary>
    Public Property DirectionVector() As ControllerVector
        Get
            Return m_DirectionVector
        End Get
        Set
            m_DirectionVector = Value
        End Set
    End Property
    Private m_DirectionVector As ControllerVector

    ''' <summary>
    ''' Handle to the actual controller HidDevice
    ''' </summary>
    Private Property deviceHandle() As HidDevice
        Get
            Return m_deviceHandle
        End Get
        Set
            m_deviceHandle = Value
        End Set
    End Property
    Private m_deviceHandle As HidDevice

    ''' <summary>
    ''' Initializes a new instance of the XboxHidController class from a 
    ''' HidDevice handle
    ''' </summary>
    ''' <param name="deviceHandle">Handle to the HidDevice</param>
    Public Sub New(deviceHandle As HidDevice)
        Me.deviceHandle = deviceHandle
        AddHandler deviceHandle.InputReportReceived, AddressOf inputReportReceived
        Me.DirectionVector = New ControllerVector() With {
                .Direction = ControllerDirection.None,
                .Magnitude = 10000
            }
        For Each direction As Object In [Enum].GetValues(GetType(ControllerDirection))
            Me.MaxMagnitude(CType(direction, ControllerDirection)) = 0
        Next
    End Sub

    ''' <summary>
    ''' Handler for processing/filtering input from the controller
    ''' </summary>
    ''' <param name="sender">HidDevice handle to the controller</param>
    ''' <param name="args">InputReport received from the controller</param>
    Private Sub inputReportReceived(sender As HidDevice, args As HidInputReportReceivedEventArgs)
        Dim dPad As Integer = CInt(args.Report.GetNumericControl(&H1, &H39).Value)

        Dim newVector As New ControllerVector() With {
                .Direction = CType(dPad, ControllerDirection),
                .Magnitude = 10000
            }

        ' DPad has priority over thumb stick, only bother with thumb stick 
        ' values if DPad is not providing a value.
        If newVector.Direction = ControllerDirection.None Then
            ' If direction is None, magnitude should be 0
            newVector.Magnitude = 0

            ' Adjust X/Y so (0,0) is neutral position
            Dim stickX As Double = args.Report.GetNumericControl(&H1, &H30).Value - 32768
            Dim stickY As Double = args.Report.GetNumericControl(&H1, &H31).Value - 32768

            Dim stickMagnitude As Integer = CInt(getMagnitude(stickX, stickY))

            Try

                ' Only process if the stick is outside the dead zone
                If stickMagnitude > 0 Then
                    newVector.Direction = coordinatesToDirection(stickX, stickY)
                    newVector.Magnitude = stickMagnitude
                    If MaxMagnitude(newVector.Direction) < newVector.Magnitude Then
                        MaxMagnitude(newVector.Direction) = newVector.Magnitude
                    End If
                End If

            Catch ex As Exception

            End Try

        End If

        ' Only fire an event if the vector changed
        If Not Me.DirectionVector.Equals(newVector) Then
            Me.DirectionVector = newVector
            RaiseEvent DirectionChanged(newVector)
        End If
    End Sub

    Public MaxMagnitude As New Dictionary(Of ControllerDirection, Integer)()

    ''' <summary>
    ''' Gets the magnitude of the vector formed by the X/Y coordinates
    ''' </summary>
    ''' <param name="x">Horizontal coordinate</param>
    ''' <param name="y">Vertical coordinate</param>
    ''' <returns>True if the coordinates are inside the dead zone</returns>
    Private Shared Function getMagnitude(x As Double, y As Double) As Double
        Dim magnitude = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2))

        If magnitude < DeadzoneTolerance Then
            magnitude = 0
        Else
            ' Scale so deadzone is removed, and max value is 10000
            magnitude = ((magnitude - DeadzoneTolerance) / (32768 - DeadzoneTolerance)) * 10000
            If magnitude > 10000 Then
                magnitude = 10000
            End If
        End If

        Return magnitude
    End Function

    ''' <summary>
    ''' Converts thumbstick X/Y coordinates centered at (0,0) to a direction
    ''' </summary>
    ''' <param name="x">Horizontal coordinate</param>
    ''' <param name="y">Vertical coordinate</param>
    ''' <returns>Direction that the coordinates resolve to</returns>
    Private Shared Function coordinatesToDirection(x As Double, y As Double) As ControllerDirection
        Dim radians As Double = Math.Atan2(y, x)
        Dim orientation As Double = (radians * (180 / Math.PI))

        ' adjust so values are 0-360 rather than -180 to 180
        ' offset so the middle of each direction has a +/- 22.5 buffer
        orientation = orientation + 180 + 22.5 + 270
        ' adjust so when dividing by 45, up is 1
        ' Wrap around so that the value is 0-360
        orientation = orientation Mod 360

        ' Dividing by 45 should chop the orientation into 8 chunks, which 
        ' maps 0 to Up.  Shift that by 1 since we need 1-8.
        Dim direction As Integer = CInt((orientation / 45)) + 1

        Return CType(direction, ControllerDirection)
    End Function

    ''' <summary>
    ''' Delegate to call when a DirectionChanged event is raised
    ''' </summary>
    ''' <param name="sender"></param>
    Public Delegate Sub DirectionChangedHandler(sender As ControllerVector)

    ''' <summary>
    ''' Event raised when the controller input changes directions
    ''' </summary>
    ''' <param name="sender">Direction the controller input changed to</param>
    Public Event DirectionChanged As DirectionChangedHandler
End Class

Public Class ControllerVector
    ''' <summary>
    ''' Get what direction the controller is pointing
    ''' </summary>
    Public Property Direction() As ControllerDirection
        Get
            Return m_Direction
        End Get
        Set
            m_Direction = Value
        End Set
    End Property
    Private m_Direction As ControllerDirection

    ''' <summary>
    ''' Gets a value indicating the magnitude of the direction
    ''' </summary>
    Public Property Magnitude() As Integer
        Get
            Return m_Magnitude
        End Get
        Set
            m_Magnitude = Value
        End Set
    End Property
    Private m_Magnitude As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        'If obj Is Nothing OrElse GetType(me) <> obj.GetType() Then
        '    Return False
        'End If

        Dim otherVector As ControllerVector = TryCast(obj, ControllerVector)

        If Me.Magnitude = otherVector.Magnitude AndAlso Me.Direction = otherVector.Direction Then
            Return True
        End If

        Return False
    End Function

    ' override object.GetHashCode
    Public Overrides Function GetHashCode() As Integer
        ' disable overflow
        Dim hash As Integer = 27
        hash = (13 * hash) + Me.Direction.GetHashCode()
        hash = (13 * hash) + Me.Magnitude.GetHashCode()
        Return hash

    End Function
End Class

Public Enum ControllerDirection
    None = 0
    Up
    UpRight
    Right
    DownRight
    Down
    DownLeft
    Left
    UpLeft
End Enum
