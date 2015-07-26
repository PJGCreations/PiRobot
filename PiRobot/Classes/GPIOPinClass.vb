Imports Windows.Devices.Gpio

''' <summary>
''' Adds extra functionality to the GPIO Pin
''' </summary>
Public Class GPIOPinClass

#Region "Enums"

    ''' <summary>
    ''' The Types of Pulse Checking to carry out for the pin
    ''' </summary>
    Public Enum PulseCheck
        [None]
        [Positive]
        [Negative]
    End Enum

#End Region

#Region "Variables"

    Private _Name As String                         'The Name of the Pin
    Private _PinNo As Integer                       'The GPIO Pin Number
    Private _Pin As GpioPin                         'The Actual GPIO Pin
    Private _Direction As GpioPinDriveMode          'The Pin Direction (Input or Output)
    Private _State As GpioPinValue                  'The Pin State (High or Low)
    Private _CheckForPulse As PulseCheck            'Whether the Pin should trigger an event on a Pulse Detect
    Private _PulseCounter As Stopwatch              'Timer to Time the length of a Pulse

#End Region

#Region "Properties"

    ''' <summary>
    ''' The Name of the Pin
    ''' </summary>
    ''' <returns></returns>
    Public Property Name As String
        Get
            Return _Name
        End Get
        Set(value As String)
            _Name = value
        End Set
    End Property

    ''' <summary>
    ''' The GPIO Pin Number
    ''' </summary>
    ''' <returns></returns>
    Public Property PinNo As Integer
        Get
            Return _PinNo
        End Get
        Set(value As Integer)
            _PinNo = value
        End Set
    End Property

    ''' <summary>
    ''' The Actual GPIO Pin
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property Pin As GpioPin
        Get
            Return _Pin
        End Get

    End Property

    ''' <summary>
    ''' The Pin Direction (Input or Output)
    ''' </summary>
    ''' <returns></returns>
    Public Property Direction As GpioPinDriveMode
        Get
            Return _Direction
        End Get
        Set(value As GpioPinDriveMode)
            _Direction = value
        End Set
    End Property

    ''' <summary>
    ''' The Pin State (High or Low)
    ''' </summary>
    ''' <returns></returns>
    Public Property State As GpioPinValue
        Get

            If _Direction = GpioPinDriveMode.Input Then

                _State = _Pin.Read

            End If

            Return _State

        End Get
        Set(value As GpioPinValue)
            _State = value
            _Pin.Write(value)

        End Set
    End Property

    ''' <summary>
    ''' Whether the Pin should trigger an event on a Pulse Detect
    ''' </summary>
    ''' <returns></returns>
    Public Property CheckForPulse As PulseCheck
        Get
            Return _CheckForPulse
        End Get
        Set(value As PulseCheck)
            _CheckForPulse = value
        End Set
    End Property

#End Region

#Region "Events"

    Public Event PinValueHasChanged(ByVal CurrentState As GpioPinValue)                     'The Pin Value has Changed
    Public Event PulseDetected(ByVal sender As GPIOPinClass, ByVal Width As Double)         'A Pulse has been Detected

#End Region

#Region "Class Subs"

    ''' <summary>
    ''' GPIOPin Class Newed Up
    ''' </summary>
    Public Sub New(ByVal PinName As String, ByVal PinNumber As Integer, PinDirection As GpioPinDriveMode, PinState As GpioPinValue, CheckPulse As PulseCheck)

        Dim gpio = GpioController.GetDefault()                              'Get the Default GPIO Controller

        '
        ' Setup the Pin Properties
        '
        _Name = PinName
        _PinNo = PinNumber
        _Direction = PinDirection
        _State = PinState
        _CheckForPulse = CheckPulse

        '
        ' Open the Pin, and make sure that it then exists
        '
        _Pin = gpio.OpenPin(_PinNo)

        If _Pin Is Nothing Then

            Exit Sub

        End If

        '
        ' Set the Drive Mode, and if it's an output, set the direction
        '
        _Pin.SetDriveMode(_Direction)

        If PinDirection = GpioPinDriveMode.Output Then

            _Pin.Write(_State)

        End If

        '
        ' Ad a Hander to the GPIO Pin ValueChanged event
        '
        AddHandler _Pin.ValueChanged, AddressOf PinValueChanged

    End Sub

#End Region

    ''' <summary>
    ''' Create a small Positive Pulse on the Pin
    ''' </summary>
    Public Sub PositivePulse()

        _Pin.Write(GpioPinValue.Low)

        Task.Delay(100)

        _Pin.Write(GpioPinValue.High)

        '
        ' Approximate 10us delay
        '
        For intcounter As Integer = 0 To 50000

            'Do Nothing

        Next

        _Pin.Write(GpioPinValue.Low)


    End Sub

    ''' <summary>
    ''' The Pin Value has Changed
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub PinValueChanged(ByVal sender As Object, ByVal e As GpioPinValueChangedEventArgs)

        If (_CheckForPulse = PulseCheck.Positive And e.Edge = GpioPinEdge.RisingEdge) Or (_CheckForPulse = PulseCheck.Negative And e.Edge = GpioPinEdge.FallingEdge) Then

            _PulseCounter = New Stopwatch
            _PulseCounter.Start()

        ElseIf (_CheckForPulse = PulseCheck.Positive And e.Edge = GpioPinEdge.FallingEdge) Or (_CheckForPulse = PulseCheck.Negative And e.Edge = GpioPinEdge.RisingEdge) Then

            _PulseCounter.Stop()

            Debug.WriteLine("New Pulse Detected - Length = " & _PulseCounter.Elapsed.TotalMilliseconds & " ms")
            RaiseEvent PulseDetected(Me, _PulseCounter.Elapsed.TotalMilliseconds)

        End If

        RaiseEvent PinValueHasChanged(e.Edge)

    End Sub

    ''' <summary>
    ''' Toggle the Pin
    ''' </summary>
    Public Sub TogglePin()

        If State = GpioPinValue.High Then

            State = GpioPinValue.Low

        Else

            State = GpioPinValue.High

        End If

    End Sub

End Class
