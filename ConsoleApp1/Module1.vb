Imports System.Text.RegularExpressions

Module Module1

    Private Enum GeneratorModes
        Zero = 0
        FF = 1
        Random = 2
        Text = 3
        [Byte] = 4
        Hexa = 5
    End Enum

    Private Enum WriteMode
        Append = 0
        Overwrite = 1
    End Enum

    Private DataSize As Long = 0
    Private DataPath As String

    Private ErrorMessage As String = ""

    Private PathOK As Boolean = False
    Private SizeOK As Boolean = False
    Private GeneratorOK As Boolean = True
    Private ModeOK As Boolean = True

    Private Mode As WriteMode = WriteMode.Append
    Private WarningsEnabled As Boolean = True

    Private DataGenerator As GeneratorModes = GeneratorModes.Random
    Private DataSequence As New List(Of Byte)

    Sub Main()
        ShowBanner()
        'ShowProgressBar(1, 0, 100)
        'Exit Sub

        GreyDogSoftware.Extensions.StartupArgs.CaseSensitive = True
        Dim Args = GreyDogSoftware.Extensions.StartupArgs.CurrentArgs
        If Args.Count > 0 Then
            For Each Arg In Args
                Select Case Arg.Key.ToLower
                    Case "--help", "-?"
                        ShowHelp()
                        Exit Sub
                    Case "--size", "-s"
                        Try
                            DataSize = ParseSize(Arg.Value)
                            SizeOK = True
                        Catch ex As Exception
                            'ErrorMessage = ex.Message
                            ConsoleWriteErrorLine(ex.Message)
                            Exit Sub
                        End Try
                    Case "--out", "-o"
                        Try
                            DataPath = ParsePaths(Arg.Value)
                            PathOK = True
                        Catch ex As Exception
                            ConsoleWriteErrorLine(ex.Message)
                            Exit Sub
                        End Try
                    Case "--gen", "-g"
                        Try
                            DataGenerator = ParseGenerator(Arg.Value)
                            GeneratorOK = True
                        Catch ex As Exception
                            GeneratorOK = False
                            ConsoleWriteErrorLine(ex.Message)
                            Exit Sub
                        End Try
                    Case "--mode", "-m"
                        Try
                            Mode = ParseMode(Arg.Value)
                            ModeOK = True
                        Catch ex As Exception
                            ModeOK = False
                            ConsoleWriteErrorLine(ex.Message)
                            Exit Sub
                        End Try
                    Case "--nowarn", "-x"
                        WarningsEnabled = False
                    Case Else
                        ' ?
                End Select
            Next
        Else
            ConsoleWriteErrorLine("No arguments defined.")
            ShowHelp()
            Exit Sub
        End If

        If SizeOK And PathOK And GeneratorOK And ModeOK Then
            Dim FileTarget As IO.FileInfo = New IO.FileInfo(DataPath)
            ConsoleWriteLine("-- Current work configuration --", ConsoleColor.DarkYellow)
            ConsoleWrite("Target file:  ", ConsoleColor.DarkGreen)
            ConsoleWriteLine(FileTarget.Name, ConsoleColor.White)
            ConsoleWrite("Target size:  ", ConsoleColor.DarkGreen)
            ConsoleWrite(DataSize)
            ConsoleWriteLine(" bytes", ConsoleColor.White)
            ConsoleWrite("Target path:  ", ConsoleColor.DarkGreen)
            ConsoleWriteLine(FileTarget.DirectoryName, ConsoleColor.White)
            ConsoleWrite("Target exist: ", ConsoleColor.DarkGreen)
            ConsoleWriteLine(If(FileTarget.Exists, "Yes", "No"), ConsoleColor.White)
            ConsoleWrite("Write mode:   ", ConsoleColor.DarkGreen)
            ConsoleWriteLine(Mode.ToString, ConsoleColor.White)
            ConsoleWrite("Generator:    ", ConsoleColor.DarkGreen)
            ConsoleWriteLine(DataGenerator.ToString, ConsoleColor.White)



            Dim FileMode As IO.FileMode = If(Mode = WriteMode.Append, IO.FileMode.Append, IO.FileMode.Create)

            If WarningsEnabled Then
                If FileMode = IO.FileMode.Create Then
                    If IO.File.Exists(FileTarget.FullName) Then
                        Console.WriteLine("")
                        Console.WriteLine("The file already exists and the mode is set to overwrite.")
                        ConsoleWriteQuestion("Do you want to continue? (")
                        ConsoleWrite("y", ConsoleColor.Green)
                        Console.Write("/")
                        ConsoleWrite("n", ConsoleColor.Red)
                        ConsoleWrite("):")
                        Dim Ans As String = ""
                        While Ans <> "y" Or Ans <> "n"
                            Console.CursorLeft = 42
                            Console.Write(" ")
                            Console.CursorLeft = 42
                            Dim Response = Console.ReadKey
                            Ans = Response.KeyChar.ToString.ToLower
                            If Ans = "y" Then
                                Console.CursorTop = Console.CursorTop + 1
                                Console.CursorLeft = 0
                                Exit While
                            ElseIf Ans = "n" Then
                                Console.CursorTop = Console.CursorTop + 1
                                Console.CursorLeft = 0
                                Console.WriteLine("Nothing to do. Ending.")
                                Exit Sub
                            End If
                        End While
                    End If
                Else
                    Console.WriteLine("")
                    If IO.File.Exists(FileTarget.FullName) Then
                        ConsoleWriteInfoLine("Mode set to append. The bytes will be added to the existing file.")
                    Else
                        ConsoleWriteInfoLine("Mode set to append. The bytes will be added to a new file.")
                    End If
                End If
            End If


            '' This block is commented out so I can debug the program.
            '' This block writes the file content
            'Debug.WriteLine(Console.WindowWidth & "x" & Console.WindowHeight)
            Using FileStream As New IO.FileStream(FileTarget.FullName, FileMode)
                Using FileWriter As New IO.BinaryWriter(FileStream)
                    Console.WriteLine("")
                    ConsoleWriteInfoLine("Writing data...")
                    For I As Long = 0 To DataSize - 1
                        FileWriter.Write(GetDataByte())
                    Next
                    ConsoleWriteLine("Done!")
                End Using
            End Using
        Else
            If DataSize <= 0 Then
                ConsoleWriteError("The data size can't be zero. Please define a size and try again (Ex: ")
                ConsoleWrite("-s 1K", ConsoleColor.DarkYellow)
                ConsoleWriteLine(").")
                Exit Sub
            End If
            If String.IsNullOrEmpty(DataPath) Then
                ConsoleWriteError("The target file can't be empty. Set an output and try again (Ex: ")
                ConsoleWrite("-o somefile.txt", ConsoleColor.DarkYellow)
                ConsoleWriteLine(").")
                Exit Sub
            End If
            ConsoleWriteErrorLine(ErrorMessage)
        End If
        'Threading.Thread.Sleep(3000)
    End Sub
    Private Sub ConsoleWriteError(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[ERROR] ", ConsoleColor.DarkRed)
        Console.ForegroundColor = Color
        Console.Write(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteErrorLine(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[ERROR] ", ConsoleColor.DarkRed)
        Console.ForegroundColor = Color
        Console.WriteLine(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteInfo(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[INFO] ", ConsoleColor.DarkCyan)
        Console.ForegroundColor = Color
        Console.Write(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteInfoLine(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[INFO] ", ConsoleColor.DarkCyan)
        Console.ForegroundColor = Color
        Console.WriteLine(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteWarning(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[WARNING] ", ConsoleColor.DarkYellow)
        Console.ForegroundColor = Color
        Console.Write(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteWarningLine(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[WARNING] ", ConsoleColor.DarkYellow)
        Console.ForegroundColor = Color
        Console.WriteLine(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteQuestion(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[QUESTION] ", ConsoleColor.DarkMagenta)
        Console.ForegroundColor = Color
        Console.Write(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteQuestionLine(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        ConsoleWrite("[QUESTION] ", ConsoleColor.DarkMagenta)
        Console.ForegroundColor = Color
        Console.WriteLine(Message)
        Console.ResetColor()
    End Sub

    Private Sub ConsoleWrite(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        Console.ForegroundColor = Color
        Console.Write(Message)
        Console.ResetColor()
    End Sub
    Private Sub ConsoleWriteLine(Message As String, Optional Color As ConsoleColor = ConsoleColor.White)
        Console.ForegroundColor = Color
        Console.WriteLine(Message)
        Console.ResetColor()
    End Sub

    Private Sub ShowProgressBar(Value As Integer, Min As Integer, Max As Integer, Optional BarPos As Integer = -1)
        ' ░ ▒ ▓ █ ■

        Dim BarWidth As Integer = (Console.WindowWidth / 100) * 75
        Dim BarConst As Integer = BarWidth / 100

        Dim PBStyle As Integer = 0
        Select Case PBStyle
            Case 0
                Dim BarBack As String = ""
                For I As Int32 = 0 To BarWidth
                    BarBack &= "▒"
                Next
                Console.WriteLine(" " & BarBack & "   0%")
                Console.CursorTop = Console.CursorTop - 1
                For I As Int32 = 0 To BarWidth
                    Console.CursorLeft = I + 1
                    Console.Write("█")
                    Threading.Thread.Sleep(50)
                    Console.CursorLeft = BarWidth + 1
                    Console.Write(I.ToString.PadLeft(4, " ") & "%")
                Next
            Case 1
                Dim BarBack As String = ""
                For I As Int32 = 0 To BarWidth
                    BarBack &= " "
                Next
                Console.WriteLine(" " & BarBack & "   0%")
                Console.CursorTop = Console.CursorTop - 1
                Console.CursorLeft = 1
                For I As Int32 = 0 To 90
                    Console.Write("■")
                    Threading.Thread.Sleep(10)
                Next
            Case 2
                Console.WriteLine("                                                100%")
                Console.WriteLine(" ██████████████████████████████████████████████████████████████████████████████████████████▒▒▒▒▒▒▒▒▒▒")
                Console.WriteLine("                                       987651623/91521661637")
            Case 3
                Console.WriteLine("                                                100%")
                Console.WriteLine(" ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■")
                Console.WriteLine("                                       987651623/91521661637")
            Case Else

        End Select
    End Sub

    Private DataIndex As Long = 0
    Private RandGen As New Random
    Private Function GetDataByte() As Byte
        Select Case DataGenerator
            Case GeneratorModes.Byte, GeneratorModes.Hexa, GeneratorModes.Text
                Dim CurrentByte As Byte = DataSequence(DataIndex)
                DataIndex += 1
                If DataIndex > (DataSequence.Count - 1) Then
                    DataIndex = 0
                End If
                Return CurrentByte
            Case GeneratorModes.Zero
                Return 0
            Case GeneratorModes.FF
                Return 255
            Case Else
                Return RandGen.Next(0, 256)
        End Select
    End Function

    Private Function ParseSize(Data As List(Of String)) As Long
        If Data.Count = 1 Then
            Dim SizeRegex As String = "(?<size>[0-9]+)(?<multi>[bkgmtpeBKGMTPE]*)"
            Dim regex As Regex = New Regex(SizeRegex)
            Dim Matches As MatchCollection = regex.Matches(Data(0))
            If Matches.Count > 0 Then
                For Each Match As Match In Matches
                    Dim DSize As Int64 = Match.Groups("size").Value
                    Dim DMulti As Long = 0
                    Select Case Match.Groups("multi").Value
                        Case "b"
                            DMulti = 1
                        Case "k"
                            DMulti = 1000
                        Case "m"
                            DMulti = 1000000
                        Case "g"
                            DMulti = 1000000000
                        Case "t"
                            DMulti = 1000000000000
                        Case "p"
                            DMulti = 1000000000000000
                        Case "e"
                            DMulti = 1000000000000000000
                        Case "B"
                            DMulti = 1
                        Case "K"
                            DMulti = 1024
                        Case "M"
                            DMulti = 1048576
                        Case "G"
                            DMulti = 1073741824
                        Case "T"
                            DMulti = 1099511627776
                        Case "P"
                            DMulti = 1125899906842624
                        Case "E"
                            DMulti = 1152921504606846976
                            'DMulti = 9223372036854775807
                        Case Else
                            DMulti = 1
                    End Select
                    Dim SizeEval As System.Numerics.BigInteger
                    Try
                        SizeEval = New System.Numerics.BigInteger(DSize)
                        SizeEval = SizeEval * DMulti
                    Catch ex As Exception
                        Throw New Exception("Can't cast that data size. Try making the size a bit less.")
                    End Try
                    If SizeEval < Int64.MinValue Then
                        Throw New Exception("The size can't be negative.")
                    End If
                    If SizeEval > Int64.MaxValue Then
                        Throw New Exception("The size is too big.")
                    End If
                    DSize = SizeEval
                    Return DSize
                Next
            Else
                Throw New Exception("Invalid size")
            End If
        End If
        Throw New Exception("Size not defined")
    End Function

    Private Function ParsePaths(Data As List(Of String)) As String
        If Data.Count = 1 Then
            Dim Path As New IO.FileInfo(Data(0))
            Return Path.FullName
        ElseIf Data.Count > 1 Then
            Throw New Exception("To many outputs")
        Else
            Throw New Exception("Output not defined")
        End If
        Return ""
    End Function

    Private Function ParseGenerator(Data As List(Of String)) As GeneratorModes
        If Data.Count = 1 Then
            Select Case Data(0).ToLower
                Case "rnd"
                    Return GeneratorModes.Random
                Case "ff"
                    Return GeneratorModes.FF
                Case "zero"
                    Return GeneratorModes.Zero
                Case Else
                    '' Bellow are the "special cases". Those are custom formated generators.
                    Dim DataText As String = Data(0)
                    If DataText.IndexOf("text=") > -1 Then
                        '' A text input
                        If DataText.IndexOf("=") > -1 Then
                            DataText = DataText.Substring(DataText.IndexOf("=") + 1)
                            If DataText.Length > 0 Then
                                DataSequence.AddRange(Text.Encoding.UTF8.GetBytes(DataText))
                            Else
                                Throw New Exception("Empty text input.")
                            End If
                        Else
                            Throw New Exception("Bad text format.")
                        End If
                        Return GeneratorModes.Text
                    ElseIf DataText.IndexOf("byte=") > -1 Then
                        '' A byte input
                        DataText = DataText.Substring(DataText.IndexOf("=") + 1).Trim
                        Dim DataSplit() = DataText.Split(",")
                        If DataSplit.Count > 0 Then
                            For Each Seq As String In DataSplit
                                If IsNumeric(Seq) Then
                                    Dim SeqVal As Int64 = Int32.Parse(Seq)
                                    If SeqVal < Byte.MinValue Or SeqVal > Byte.MaxValue Then
                                        Throw New Exception("Invalid byte value in the input sequence.")
                                    Else
                                        DataSequence.Add(Byte.Parse(Seq))
                                    End If
                                End If
                            Next
                            Return GeneratorModes.Byte
                        Else
                            Throw New Exception("Empty byte array.")
                        End If
                    ElseIf DataText.IndexOf("hexa=") > -1 Then
                        '' An hexadecimal input
                        DataText = DataText.Substring(DataText.IndexOf("=") + 1).Trim
                        Dim DataSplit() = DataText.Split(",")
                        If DataSplit.Count > 0 Then
                            For Each Seq As String In DataSplit
                                Dim SeqVal As Long = 0
                                If Long.TryParse(Seq, System.Globalization.NumberStyles.HexNumber, Nothing, SeqVal) Then
                                    If SeqVal < Byte.MinValue Or SeqVal > Byte.MaxValue Then
                                        Throw New Exception("Invalid byte value in the input sequence.")
                                    Else
                                        DataSequence.Add(Byte.Parse(SeqVal.ToString))
                                    End If
                                Else
                                    Throw New Exception("Invalid value in the input sequence.")
                                End If
                            Next
                            Return GeneratorModes.Hexa
                        Else
                            Throw New Exception("Empty hexadecimal byte array.")
                        End If
                    Else
                        Throw New Exception("Unknown generator format.")
                    End If
            End Select
        ElseIf Data.Count > 1 Then
            Throw New Exception("Make your mind and select just one data generator")
        End If
        Return GeneratorModes.Random
    End Function

    Private Function ParseMode(Data As List(Of String)) As WriteMode
        If Data.Count = 1 Then
            Select Case Data(0).ToLower.Trim
                Case "append"
                    Return WriteMode.Append
                Case "overwrite"
                    Return WriteMode.Overwrite
                Case Else
                    Throw New Exception("Unknown file write mode. Try append or overwrite.")
            End Select
        Else
            Throw New Exception("The fuck are you trying to do?. Pick only one.")
        End If
    End Function

    Private Sub ShowBanner()
        Console.WriteLine("")
        ConsoleWriteLine("  ------------------------", ConsoleColor.Yellow)
        ConsoleWriteLine("  Dummy Files maker v0.1.2", ConsoleColor.Yellow)
        ConsoleWriteLine("  ------------------------", ConsoleColor.Yellow)
        Console.WriteLine("")
    End Sub

    Private Sub ShowHelp()
        Console.WriteLine("")


        ConsoleWrite("--help, -?:", ConsoleColor.DarkCyan)
        ConsoleWriteLine("   Prints this message.")
        Console.WriteLine("")


        ConsoleWrite("--nowarn, -x:", ConsoleColor.DarkCyan)
        ConsoleWriteLine(" Disables the warning in overwrite mode.")
        Console.WriteLine("")


        ConsoleWrite("--out, -o:", ConsoleColor.DarkCyan)
        ConsoleWriteLine("    Set the output file.")
        ConsoleWriteLine("              Ex: --out some_file.txt", ConsoleColor.DarkYellow)
        Console.WriteLine("")


        ConsoleWrite("--size, -s:", ConsoleColor.DarkCyan)
        ConsoleWriteLine("   Set the output file size. If the unit isn't specified, then the value")
        ConsoleWriteLine("              will be set in bytes.")
        ConsoleWriteLine("              Ex: --size 1024 or --size 1K", ConsoleColor.DarkYellow)
        Console.WriteLine("              1000 bytes     1024 bytes")
        Console.WriteLine("              b: byte        B:byte")
        Console.WriteLine("              k: Kibibyte    K:Kilobyte")
        Console.WriteLine("              m: Kibibyte    M:Megabyte")
        Console.WriteLine("              g: Gibibyte    G:Gigabyte")
        Console.WriteLine("              t: Tebibyte    T:Terabyte")
        Console.WriteLine("              p: Pebibyte    P:Petabyte")
        Console.WriteLine("              e: Exbibyte    E:Exabyte")
        Console.WriteLine("")


        ConsoleWrite("--gen, -g:", ConsoleColor.DarkCyan)
        ConsoleWriteLine("    Specifies the filler generator.")
        ConsoleWriteLine("              Ex: --gen hexa=""4e,65,76,65,72,20,67,6f,6e,6e,61,20,67,69,76,65,20,79,6f,75,20,75,70,21,20""", ConsoleColor.DarkYellow)
        Console.WriteLine("              rnd (default):        Random data.")
        Console.WriteLine("              zero:                 All bytes are zero.")
        Console.WriteLine("              ff:                   All bytes are FF (255).")
        Console.WriteLine("              text=""some text"":     Repeats the text value.")
        Console.WriteLine("              byte=""0,127,255"":     Repeats the byte sequence.")
        Console.WriteLine("              hexa=""0,A0,FF"":       Repeats the byte sequence in hex format.")
        Console.WriteLine("")


        ConsoleWrite("--mode, -m:", ConsoleColor.DarkCyan)
        ConsoleWriteLine("   Sets the writing mode.")
        ConsoleWriteLine("              Ex: --mode overwrite", ConsoleColor.DarkYellow)
        Console.WriteLine("              append (default):     Appends the data at the end of the file")
        Console.WriteLine("              overwrite:            Writes data to the specified file. If the file")
        Console.WriteLine("                                    already exist. Overwrites the current data.")
    End Sub

End Module
