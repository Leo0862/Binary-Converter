using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;

namespace BinaryConverter
{

    public class BinaryConverterUnit
    {
        public int Position { get; set; }
        public bool Checked { get; set; }
    }

    public class BinaryConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type Target_Type, object Parameter, CultureInfo culture)
        {
            BinaryConverterUnit bitMaskUnit = new BinaryConverterUnit();
            bitMaskUnit.Position = (int)values[0];
            bitMaskUnit.Checked = (bool)values[1];
            return bitMaskUnit;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HexStringConverter : IValueConverter
    {
        private string? lastValidValue;
        public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? ret = null;

            if (value is not null and byte)
            {
                byte valueAsString = (byte)value;
                ret = valueAsString.ToString("X");
            }

            return ret;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            byte? ret = null;
            string? valueAsString = ((string)value).Replace(" ", String.Empty).ToUpper();
            if (value != null && value is string)
            {
                try
                {
                    if (((string)value).Length > 2)
                    {
                        valueAsString = valueAsString[^2..];
                    }
                    lastValidValue = IsHex(valueAsString) ? valueAsString : lastValidValue;

                    ret = System.Convert.ToByte(lastValidValue, 16);
                }
                catch (FormatException)
                {
                    ret = 255;
                }
                catch (OverflowException)
                {
                    ret = 255;
                }
                catch (ArgumentOutOfRangeException)
                {
                    ret = 0;
                }
            }


            return ret;
        }


        private bool IsHex(string text)
        {
            var reg = new System.Text.RegularExpressions.Regex("^[0-9A-Fa-f]{1,2}$");
            return reg.IsMatch(text);
        }
    }

    public static class BitArrayExtensions
    {
        public static byte ToByte(this BitArray binary)
        {
            if (binary == null)
                throw new ArgumentNullException("binary");
            if (binary.Length > 8)
                throw new ArgumentException("must be at most 8 bits long");

            byte[] result = new byte[1];
            binary.CopyTo(result, 0);
            return result[0];
        }

        public static int ToInt(this BitArray binary)
        {
            if (binary == null)
                throw new ArgumentNullException("binary");
            if (binary.Length > 32)
                throw new ArgumentException("must be at most 32 bits long");

            int[] result = new int[1];
            binary.CopyTo(result, 0);
            return result[0];
        }
        public static string ToHexString(this BitArray binary)
        {
            byte result = binary.ToByte();
            return result.ToString("X2");
        }

        public static string ToBitString(this BitArray binary, bool sep)
        {
            if (binary == null)
                throw new ArgumentNullException("binary");
            if (binary.Length > 8)
                throw new ArgumentException("must be at most 8 bits long");

            StringBuilder sb = new StringBuilder();
            for (int idx = binary.Length - 1; idx >= 0; idx--)
            {
                sb.Append(binary[idx] == true ? "1" : "0");
                if (idx == 4 && sep == true)
                {
                    sb.Append("_");
                }
            }

            return sb.ToString();
        }
    }

    public class BinaryConverterViewModel : ObservableValidator
    {
        public BinaryConverterViewModel()
        {
            maskChanngedCommand = new RelayCommand<BinaryConverterUnit>(MaskChannged);
            setAllBitsCommand = new RelayCommand(SetAllBits);
            clearAllBitsCommand = new RelayCommand(ClearAllBits);
        }

        private BitArray bitMask = new(8, false);
        public string BitMaskBitString => BitMask.ToBitString(true);
        public BitArray InvertedBitMask => (new BitArray(bitMask)).Not();
        public string InvertedBitMaskBitString => InvertedBitMask.ToBitString(true);

        
        private RelayCommand<BinaryConverterUnit> maskChanngedCommand;
        public ICommand MaskChanngedCommand => maskChanngedCommand;

        private RelayCommand setAllBitsCommand;
        public ICommand SetAllBitsCommand => setAllBitsCommand;

        private RelayCommand clearAllBitsCommand;
        public ICommand ClearAllBitsCommand => clearAllBitsCommand;

        public BitArray BitMask
        {
            get
            {
                return bitMask;
            }
            set
            {
                SetProperty(ref bitMask, value);
                OnPropertyChanged(nameof(MaskDecValue));
                OnPropertyChanged(nameof(BitMaskBitString));
                OnPropertyChanged(nameof(InvertMaskDecValue));
                OnPropertyChanged(nameof(InvertedBitMaskBitString));
            }
        }

        [Range(0, 255)]
        [RegularExpression(@"^[0-9A-Fa-f]{1,2}$")]
        public byte MaskDecValue
        {
            get
            {
                return bitMask.ToByte();
            }
            set
            {
                var bitArray = new BitArray(new byte[] { value });
                //SetProperty(ref bitMask, bitArray, true, "BitMask");
                IReadOnlyCollection<ValidationResult> validationResults;
                var result = TrySetProperty(ref bitMask, bitArray, out validationResults, nameof(BitMask));
                if (result == true)
                {
                    //OnPropertyChanged(nameof(BitMask));
                    OnPropertyChanged(nameof(MaskDecValue));
                    OnPropertyChanged(nameof(BitMaskBitString));
                    OnPropertyChanged(nameof(InvertMaskDecValue));
                    OnPropertyChanged(nameof(InvertedBitMaskBitString));
                }
            }
        }
        
        public byte InvertMaskDecValue
        {
            get
            {
                return (byte)(255 - bitMask.ToByte());
            }
        }

        private void MaskChannged(BinaryConverterUnit? arg)
        {
            if (arg != null)
            {
                BinaryConverterUnit bitMaskUnit = (BinaryConverterUnit)arg;
                BitMask.Set(bitMaskUnit.Position, bitMaskUnit.Checked);

                OnPropertyChanged(nameof(BitMask));
                OnPropertyChanged(nameof(MaskDecValue));
                OnPropertyChanged(nameof(BitMaskBitString));
                OnPropertyChanged(nameof(InvertMaskDecValue));
                OnPropertyChanged(nameof(InvertedBitMaskBitString));
            }
        }

        private byte ByteStringToHex(string? byteString)
        {
            byte value = 0;
            value = Convert.ToByte(byteString, 16);

            return value;
        }

        private void SetAllBits()
        {
            BitMask.Or(new BitArray(8, true));

            OnPropertyChanged(nameof(BitMask));
            OnPropertyChanged(nameof(MaskDecValue));
            OnPropertyChanged(nameof(BitMaskBitString));
            OnPropertyChanged(nameof(InvertMaskDecValue));
            OnPropertyChanged(nameof(InvertedBitMaskBitString));
        }
        private void ClearAllBits()
        {
            BitMask.And(new BitArray(8, false));
            OnPropertyChanged(nameof(BitMask));
            OnPropertyChanged(nameof(MaskDecValue));
            OnPropertyChanged(nameof(BitMaskBitString));
            OnPropertyChanged(nameof(InvertMaskDecValue));
            OnPropertyChanged(nameof(InvertedBitMaskBitString));
        }

        public static ValidationResult? ByteStringToHex(string value, ValidationContext context)
        {
            var instance = (BinaryConverterViewModel)context.ObjectInstance;

            bool isValid = System.Text.RegularExpressions.Regex.IsMatch(value, @"[0-9a-fA-F]{1,2}");
            isValid &= value.Length == 2;

            if (isValid)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult("Must be hexadecimal digit");

        }
    }
}
