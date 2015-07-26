Imports Windows.Devices.HumanInterfaceDevice
Imports Windows.Devices.Enumeration
Imports Windows.Devices.Gpio

Public Enum MotorDirection
    [Stopped]
    [Forwards]
    [Left]
    [Right]
    [Backwards]
End Enum

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MainPage
    Inherits Page

    Private RangerControl As Ranger

    Private GPIOPinsList As Collection(Of GPIOPinClass)         'Our List of GPIO Pins

    Private Const FrontLEDPinNo As Integer = 22
    Private Const RearLEDPinNo As Integer = 23
    Private Const IRLeftPinNo As Integer = 4
    Private Const IRRightPinNo As Integer = 25
    Private Const MotorLeft1PinNo As Integer = 16
    Private Const MotorLeft2PinNo As Integer = 12
    Private Const MotorRight1PinNo As Integer = 6
    Private Const MotorRight2PinNo As Integer = 5
    Private Const SonarTriggerPinNo As Integer = 24
    Private Const SonarEchoPinNo As Integer = 13

    Dim mygpiocontroller As GpioController = GpioController.GetDefault()

    Dim Distance As Double = 0
    Dim Range As Integer = 0

    Dim CurrentMotorDirection As MotorDirection

    Dim controller As XboxHidController


#Region "Class Subs"

    ''' <summary>
    ''' Class Newed up
    ''' </summary>
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        GetXboxDevice()

        CheckAndEnableGPIO()

        textBox.Focus(FocusState.Keyboard)

    End Sub

#End Region

    Private Async Sub GetXboxDevice()

        Dim strDeviceSelector As String

        strDeviceSelector = HidDevice.GetDeviceSelector(&H01, &H05)
        Dim myDeviceInformationCollection As Windows.Devices.Enumeration.DeviceInformationCollection = Await DeviceInformation.FindAllAsync(strDeviceSelector)

        Debug.WriteLine(myDeviceInformationCollection.Count)

        For Each d As DeviceInformation In myDeviceInformationCollection

            Dim myHidDevice As HidDevice = Await HidDevice.FromIdAsync(d.Id, Windows.Storage.FileAccessMode.Read)

            If myHidDevice Is Nothing Then

                Debug.WriteLine("Cannot connect to device")

            End If

            controller = New XboxHidController(myHidDevice)

            AddHandler controller.DirectionChanged, AddressOf Controller_DirectionChanged

        Next



    End Sub

    Private Sub Controller_DirectionChanged(sender As ControllerVector)
        Debug.WriteLine($"Direction: { sender.Direction }, Magnitude: {sender.Magnitude}")
        XBoxToRobotDirection(If((sender.Magnitude < 2500), ControllerDirection.None, sender.Direction), sender.Magnitude)

        'MotorCtrl.speedValue = sender.Magnitude
    End Sub

    Private Sub XBoxToRobotDirection(ByVal dir As ControllerDirection, ByVal magnitude As Integer)

        Select Case dir
            Case ControllerDirection.Up, ControllerDirection.UpLeft, ControllerDirection.UpRight

                GoForwards()

            Case ControllerDirection.Down, ControllerDirection.DownLeft, ControllerDirection.DownRight

                GoBackwards()

            Case ControllerDirection.Left

                TurnLeft()

            Case ControllerDirection.Right

                TurnRight()

            Case ControllerDirection.None

                StopRobot()

        End Select
    End Sub

#Region "Motor Subs"

    ''' <summary>
    ''' Move the Robot Forwards
    ''' </summary>
    Private Sub GoForwards()

        CurrentMotorDirection = MotorDirection.Forwards

        GPIOPinsList(2).State = GpioPinValue.High
        GPIOPinsList(3).State = GpioPinValue.Low

        GPIOPinsList(4).State = GpioPinValue.High
        GPIOPinsList(5).State = GpioPinValue.Low

    End Sub

    ''' <summary>
    ''' Move the Robot Backwards
    ''' </summary>
    Private Sub GoBackwards()

        CurrentMotorDirection = MotorDirection.Backwards

        GPIOPinsList(2).State = GpioPinValue.Low
        GPIOPinsList(3).State = GpioPinValue.High

        GPIOPinsList(4).State = GpioPinValue.Low
        GPIOPinsList(5).State = GpioPinValue.High

    End Sub

    ''' <summary>
    ''' Turn the Robot Left
    ''' </summary>
    Private Sub TurnLeft()

        CurrentMotorDirection = MotorDirection.Left

        GPIOPinsList(2).State = GpioPinValue.Low
        GPIOPinsList(3).State = GpioPinValue.High

        GPIOPinsList(4).State = GpioPinValue.High
        GPIOPinsList(5).State = GpioPinValue.Low


    End Sub

    ''' <summary>
    ''' Turn the Robot Right
    ''' </summary>
    Private Sub TurnRight()

        CurrentMotorDirection = MotorDirection.Right

        GPIOPinsList(2).State = GpioPinValue.High
        GPIOPinsList(3).State = GpioPinValue.Low

        GPIOPinsList(4).State = GpioPinValue.Low
        GPIOPinsList(5).State = GpioPinValue.High

    End Sub

    ''' <summary>
    ''' Stop the Robot
    ''' </summary>
    Private Sub StopRobot()

        CurrentMotorDirection = MotorDirection.Stopped

        GPIOPinsList(2).State = GpioPinValue.Low
        GPIOPinsList(3).State = GpioPinValue.Low

        GPIOPinsList(4).State = GpioPinValue.Low
        GPIOPinsList(5).State = GpioPinValue.Low

    End Sub

#End Region

    ''' <summary>
    ''' A key has been pressed... Move the Robot
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub textBox_KeyUp(sender As Object, e As KeyRoutedEventArgs) Handles textBox.KeyUp
        Select Case e.Key
            Case Windows.System.VirtualKey.Up

                GoForwards()

            Case Windows.System.VirtualKey.Down

                GoBackwards()

            Case Windows.System.VirtualKey.Left

                TurnLeft()

            Case Windows.System.VirtualKey.Right

                TurnRight()

            Case Windows.System.VirtualKey.Space

                StopRobot()

        End Select
    End Sub

#Region "GPIO Subs"

    ''' <summary>
    ''' Check that the GPIO API and functionality Exists
    ''' </summary>
    ''' <returns></returns>
    Private Function CheckAndEnableGPIO() As Boolean

        Dim api = "Windows.Devices.Gpio.GpioController"

        If Metadata.ApiInformation.IsTypePresent(api) = True Then

            Dim gpio = GpioController.GetDefault()

            ' There Is no GPIO controller
            If gpio Is Nothing Then

                Return False

            End If

            CreateGPIOPins()        'Create the GPIO Pins

            RangerControl = New Ranger(GPIOPinsList(0), GPIOPinsList(1), Me.Dispatcher)

            AddRangerHandlers()

        Else

            Return False

        End If

        Return True

    End Function

    ''' <summary>
    ''' Create the GPIO Pin List
    ''' </summary>
    Public Sub CreateGPIOPins()

        GPIOPinsList = New Collection(Of GPIOPinClass)

        Dim NewGPIOPin As GPIOPinClass

        NewGPIOPin = New GPIOPinClass("Trigger", SonarTriggerPinNo, GpioPinDriveMode.Output, GpioPinValue.Low, GPIOPinClass.PulseCheck.None)
        GPIOPinsList.Add(NewGPIOPin)

        NewGPIOPin = New GPIOPinClass("Echo", SonarEchoPinNo, GpioPinDriveMode.Input, GpioPinValue.Low, GPIOPinClass.PulseCheck.None)
        GPIOPinsList.Add(NewGPIOPin)

        NewGPIOPin = New GPIOPinClass("MotorL1", MotorLeft1PinNo, GpioPinDriveMode.Output, GpioPinValue.High, GPIOPinClass.PulseCheck.None)
        GPIOPinsList.Add(NewGPIOPin)

        NewGPIOPin = New GPIOPinClass("MotorL2", MotorLeft2PinNo, GpioPinDriveMode.Output, GpioPinValue.High, GPIOPinClass.PulseCheck.None)
        GPIOPinsList.Add(NewGPIOPin)

        NewGPIOPin = New GPIOPinClass("MotorR1", MotorRight1PinNo, GpioPinDriveMode.Output, GpioPinValue.High, GPIOPinClass.PulseCheck.None)
        GPIOPinsList.Add(NewGPIOPin)

        NewGPIOPin = New GPIOPinClass("MotorR2", MotorRight2PinNo, GpioPinDriveMode.Output, GpioPinValue.High, GPIOPinClass.PulseCheck.None)
        GPIOPinsList.Add(NewGPIOPin)

        '
        ' Let Inputs Settle
        '
        Task.Delay(2000)

    End Sub

#End Region

#Region "Range Sensor Subs"

    ''' <summary>
    ''' Add the Event Handlers
    ''' </summary>
    Private Sub AddRangerHandlers()

        AddHandler RangerControl.DistanceUpdated, AddressOf DistanceUpdated
        AddHandler RangerControl.RangeUpdated, AddressOf RangeUpdated

    End Sub

    ''' <summary>
    ''' Remove any Event Handlers
    ''' </summary>
    Private Sub RemoveRangerHandlers()

        RemoveHandler RangerControl.DistanceUpdated, AddressOf DistanceUpdated
        RemoveHandler RangerControl.RangeUpdated, AddressOf RangeUpdated

    End Sub

    ''' <summary>
    ''' The Distance has been updated
    ''' </summary>
    ''' <param name="NewDistance"></param>
    Private Sub DistanceUpdated(ByVal NewDistance As Double)

        UpdateDistance(NewDistance)

    End Sub

    ''' <summary>
    ''' Update the Distance in a Threadsafe way
    ''' </summary>
    ''' <param name="NewDistance"></param>
    Private Async Sub UpdateDistance(ByVal NewDistance As Double)


        Await Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()

                                                                                        Distance = NewDistance

                                                                                    End Sub)

    End Sub

    ''' <summary>
    ''' The Range has been Updated
    ''' </summary>
    ''' <param name="NewRange"></param>
    Private Sub RangeUpdated(ByVal NewRange As Integer)

        UpdateRange(NewRange)

    End Sub

    ''' <summary>
    ''' Update the Range in a Threadsafe way.
    ''' </summary>
    ''' <param name="NewRange"></param>
    Private Async Sub UpdateRange(ByVal NewRange As Integer)

        Debug.WriteLine("New Range = " & NewRange)
        Debug.WriteLine("Current motor Direction = " & CurrentMotorDirection)

        Await Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Async Sub()

                                                                                        Range = NewRange

                                                                                        Select Case NewRange

                                                                                            Case 0



                                                                                            Case 1

                                                                                                If CurrentMotorDirection = MotorDirection.Forwards Then

                                                                                                    TurnLeft()
                                                                                                    Await Task.Delay(500)
                                                                                                    GoForwards()

                                                                                                Else

                                                                                                    Debug.WriteLine("No moving Forwards!")
                                                                                                End If

                                                                                            Case 2


                                                                                            Case 3


                                                                                            Case Else


                                                                                        End Select

                                                                                    End Sub)


    End Sub

#End Region

End Class
