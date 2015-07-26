Imports PiRanger
Imports Windows.UI.Core

Public Class Ranger

#Region "Structures"

    ''' <summary>
    ''' Distance and Range Structure
    ''' </summary>
    Public Structure DistanceAndRange
        Public Distance As Double
        Public Range As Integer

    End Structure

#End Region

#Region "Variables"

    Private RangeTimer As DispatcherTimer                       'The Timer to Initiate the Range Sensor Detection Start
    Private _TriggerPin As GPIOPinClass
    Private _EchoPin As GPIOPinClass

    Private _LastDuration As Double
    Private _LastDistance As Double
    Private _LastRange As Integer

    Private _EventsList As Queue(Of DistanceAndRange) = New Queue(Of DistanceAndRange)        'The unsent Distances and Ranges

    Private _AppDispatcher As CoreDispatcher                    'Allows us to easily run Async Calls

#End Region

#Region "Properties"

    Public Property LastDistance As Double
        Get
            Return _LastDistance
        End Get
        Set(value As Double)
            _LastDistance = value
        End Set
    End Property

    Public Property LastRange As Integer
        Get
            Return _LastRange
        End Get
        Set(value As Integer)
            _LastRange = value
        End Set
    End Property

    Public Property LastDuration As Double
        Get
            Return _LastDuration
        End Get
        Set(value As Double)
            _LastDuration = value
        End Set
    End Property

    Public Property EventsList As Queue(Of DistanceAndRange)
        Get
            Return _EventsList
        End Get
        Set(value As Queue(Of DistanceAndRange))
            _EventsList = value
        End Set
    End Property

#End Region

#Region "Public Events"

    Public Event DistanceUpdated(ByVal Distance As Double)
    Public Event RangeUpdated(ByVal Range As Integer)

#End Region

    ''' <summary>
    ''' Class newed up
    ''' </summary>
    ''' <param name="TriggerPin"></param>
    ''' <param name="EchoPin"></param>
    Public Sub New(ByRef TriggerPin As GPIOPinClass, ByRef EchoPin As GPIOPinClass, ByRef MainDispatcher As CoreDispatcher)

        _TriggerPin = TriggerPin
        _EchoPin = EchoPin
        _AppDispatcher = MainDispatcher

        '
        ' Add the Pulse Detected Event Handler
        '
        AddHandler _EchoPin.PulseDetected, AddressOf PulseDetected

        StartRangeTimer()       'Start the Range Sensor Timer

    End Sub


    ''' <summary>
    ''' Start the Range Sensor Timer
    ''' </summary>
    Private Sub StartRangeTimer()

        RangeTimer = New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 500)}

        AddHandler RangeTimer.Tick, AddressOf StartGetRange

        RangeTimer.Start()

    End Sub

    ''' <summary>
    ''' Initiate a Range Test via a 10us minimum width High Going Pulse
    ''' </summary>
    Private Async Sub StartGetRange(ByVal sender As Object, ByVal e As Object)

        Await _AppDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                         _TriggerPin.PositivePulse()

                                                                                         Dim stpStopWatch As New Stopwatch

                                                                                         stpStopWatch.Start()

                                                                                         Do While _EchoPin.State <> Windows.Devices.Gpio.GpioPinValue.High

                                                                                             If stpStopWatch.Elapsed.TotalMilliseconds > 500 Then Exit Sub

                                                                                         Loop

                                                                                         Do While _EchoPin.State = Windows.Devices.Gpio.GpioPinValue.High

                                                                                             If stpStopWatch.Elapsed.TotalMilliseconds > 500 Then Exit Sub

                                                                                         Loop

                                                                                         stpStopWatch.Stop()

                                                                                         PulseDetected(_EchoPin, stpStopWatch.Elapsed.TotalMilliseconds)

                                                                                     End Sub)

    End Sub

    ''' <summary>
    ''' A Pulse has been detected by the Range Sensor
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="Duration"></param>
    Private Sub PulseDetected(ByVal sender As GPIOPinClass, ByVal Duration As Double)

        Debug.WriteLine(Duration)
        _LastDuration = Duration

        '
        ' Convert The Pulse Duration to a distance...
        '
        Dim Distance As Double = Duration * 17150

        '
        ' Convert for Centimetersand round
        '
        Distance /= 1000
        Distance = Math.Round(Distance, 2)

        _LastDistance = Distance

        If SortOutRanges(_LastDistance) = True Then          'If the Range has changed, then we need to record an event

            Debug.WriteLine("Range Updated in Ranger = " & _LastRange)
            _EventsList.Enqueue(New DistanceAndRange With {.Distance = _LastDistance, .Range = _LastRange})
            RaiseEvent RangeUpdated(_LastRange)

        End If

        RaiseEvent DistanceUpdated(_LastDistance)

    End Sub

    ''' <summary>
    ''' Sort out the Range from the current Distance
    ''' </summary>
    ''' <param name="Distance"></param>
    ''' <returns>False if the Range hasn't Changed, True if it has</returns>
    Private Function SortOutRanges(ByVal Distance As Double) As Boolean

        Select Case CInt(Distance)

            Case 0

                'Do Nothing

            Case 1 To 10

                If _LastRange <> 0 Then

                    _LastRange = 0

                    Return True                     'Range has Changed

                End If

            Case 11 To 30

                If _LastRange <> 1 Then

                    _LastRange = 1

                    Return True                     'Range has Changed

                End If

            Case 31 To 50

                If _LastRange <> 2 Then

                    _LastRange = 2

                    Return True                     'Range has Changed

                End If

            Case 51 To 100

                If _LastRange <> 3 Then

                    _LastRange = 3

                    Return True                     'Range has Changed

                End If

            Case Else

                If _LastRange <> -1 Then

                    _LastRange = -1

                    Return True                     'Range has Changed

                End If

        End Select

        Return False                                'Range hasn't changed

    End Function

End Class
