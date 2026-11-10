#region Usings
#endregion

namespace SF.Models;

public static class IdGenerator
{
    private static char _minChar = 'A';
    private static char _maxChar = 'Z';
    private static int _minDigit = 0;
    private static int _maxDigit = 9;
    private static int _minLength = 1;
    private static int _currentLength = 0;

    //start at 25 characters which is more than enough for most autoids
    private static char[] _currentBuffer = new char[25];

    private static int _minNumId = 0;

    /// <summary>
    /// Get the next Id in the sequence
    /// </summary>
    /// <returns>The Next Alphanumeric Id</returns>
    public static string NextId()
    {
        //if the current length is less than 1 it means the generator was reset
        if(_currentLength < 1)
        {
            _currentLength = _minLength > 0 ? _minLength - 1 : 0;
            ResetSequenceAndIncreaseLength();
            return CreateString();
        }

        //last character's index
        var lastIndex = _currentLength - 1;

        //start at the last character
        var i = lastIndex;

        //iterate backwards
        for(; i > -1; i--)
        {
            var currentChar = _currentBuffer[i];

            //if char is a number
            if(CharIsDigit(currentChar))
            {
                //get the numeric value and increment
                var currentDigit = CharToDigit(currentChar) + 1;

                if(currentDigit <= _maxDigit)
                {
                    //at this point we know the character is no over the max
                    //so just assign it and exit the loop because no more characters need to be incremented
                    _currentBuffer[i] = DigitToChar(currentDigit);
                    break;
                }

                //if the current character is greater than the max digit
                //reset the character if it is the last digit to the right
                //if it is not the last digit then continue with letters
                _currentBuffer[i] = _minChar;

                break;
            }

            //increment the character which at this point is a letter
            currentChar++;

            //at this point we know the currentChar is not a digit but actually a letter
            if(currentChar <= _maxChar)
            {
                //at this point we know the character is not over the max
                //so just assign it and exit the loop because no more characters need to be incremented
                _currentBuffer[i] = currentChar;
                break;
            }

            //if the current character is greater than the max letter
            //reset the character if it is the first digit to the left
            //if it is not the first digit to the left then reset to a number and continue
            if(i != 0)
            {
                //if the character is not the first digit reset it to min digit
                _currentBuffer[i] = DigitToChar(_minDigit);
                continue;
            }

            _currentBuffer[i] = _minChar;
        }

        //if the last characters processed and reset
        //then we need to determine if we need to insert a new value
        if(i < 0)
        {
            ResetSequenceAndIncreaseLength();
        }

        return CreateString();
    }

    /// <summary>
    /// Get the next numeric id. This is just an incremental integer
    /// </summary>
    /// <returns>Next integer</returns>
    public static int NextNumericID()
    {
        _minNumId++;
        return _minNumId;
    }

    /// <summary>
    /// Seeds the generator. It can be used to restart a sequence where it left off.
    /// </summary>
    /// <param name="seedId">Must contain A-Z and 0-9</param>
    public static void Seed(string seedId)
    {
        ResetConfiguration();

        for(var i = 0; i < seedId.Length; i++)
        {
            _currentBuffer[i] = seedId[i];
        }

        _currentLength = seedId.Length;

    }

    /// <summary>
    /// Restarts the id sequence from the beginning
    /// </summary>
    public static void Reset()
    {
        _minNumId = 0;
        _currentLength = 0;
    }

    /// <summary>
    /// Configures the id generation parameters used and restarts the id sequence.
    /// </summary>
    /// <param name="minChar">Character from A-Z</param>
    /// <param name="maxChar">Character from A-Z</param>
    /// <param name="minDigit">Digit from 0-9</param>
    /// <param name="maxDigit">Digit from 0-9</param>
    /// <param name="minLength">Any number greater than 0</param>
    public static void Configure(char minChar, char maxChar, int minDigit, int maxDigit, int minLength)
    {
        _minChar = minChar;
        _maxChar = maxChar;
        _minDigit = minDigit;
        _maxDigit = maxDigit;
        _minLength = minLength;
        Reset();
    }

    /// <summary>
    /// Resets to the default configuration: A-Z, 0-9, min length 2 and restarts the id sequence.
    /// </summary>
    public static void ResetConfiguration()
    {
        _minChar = 'A';
        _maxChar = 'Z';
        _minDigit = 0;
        _maxDigit = 9;
        _minLength = 0;
        Reset();
    }

    #region Helpers
    private static void ResetSequenceAndIncreaseLength()
    {
        _currentLength++;

        //if the new length is greater than the buffer then grow the buffer to double size
        if(_currentLength > _currentBuffer.Length)
        {
            var oldBuffer = _currentBuffer;

            _currentBuffer = new char[oldBuffer.Length * 2];

            oldBuffer.CopyTo(_currentBuffer, 0);
        }

        //reset the first character to the min char
        _currentBuffer[0] = _minChar;

        //reset the other chars to min digit
        var minDigitAsChar = DigitToChar(_minDigit);
        for(var i = 1; i < _currentLength; i++)
        {
            _currentBuffer[i] = minDigitAsChar;
        }
    }
    private static bool CharIsDigit(char c)
    {
        //the ascii range for digits is 48-57 inclusive
        if(c >= 48)
            return c <= 57;
        else
            return false;
    }
    private static char DigitToChar(int d)
    {
        return (char)(d + 48);
    }
    private static int CharToDigit(char c)
    {
        return c - '0';
    }
    private static string CreateString()
    {
        return new string(_currentBuffer, 0, _currentLength);
    }
    #endregion
}
