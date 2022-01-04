﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StripV3Consent.Model
{
    public class ImportFile : DataFile
    {
        public ImportFile(string path) : base(path)
        {
        }

        public FileValidationState IsValid
        {
            get
            {
                FileValidationState ReturnValue = new FileValidationState();

                if (File.Extension != ".csv") { return new FileValidationState() { IsValid = ValidState.Error, Message = "File not CSV type" }; };

                string[] SpecificationFileNames = Spec2021K.Specification.PatientFiles.Select(SpecificationFile => SpecificationFile.SimplifiedName).ToArray();

                //If any of the words from 2021K's filenames (patient, consent, contact, admission) are in the current filename
                if (!SpecificationFileNames.Select(SpecificationFileName => File.Name.Contains(SpecificationFileName)).Contains(true))
                {
                    return new FileValidationState() { IsValid = ValidState.Warning, Message = "File name not in expected list of file names" };
                }

                if (IsCommaDelimited() != true)
                {
                    return new FileValidationState() { IsValid = ValidState.Error, Message = "CSV file not comma separated" };
                }


                return new FileValidationState() { IsValid = ValidState.Good, Message = "File passed validation checks" };
            }
        }

        public bool ContainsHeaders
        {
            get
            {
                String TopLeftValue = null;
                using (StreamReader StreamReader = new StreamReader(this.Path))
                {
                    StringBuilder TopLeftValueBuilder = new StringBuilder();
                    while ((char)StreamReader.Peek() != ',')
                    {
                        TopLeftValueBuilder.Append((char)StreamReader.Read());
                    }
                    TopLeftValue = TopLeftValueBuilder.ToString();
                }

                if (TopLeftValue.StartsWith("HEADER_"))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        public bool IsCommaDelimited()
        {
            //Crudely tries to find if the file is comma delimited by seeing what is the most common char out of common delimiters
            //Especially tab as excel loves to swap commas for tabs in csv files
            char[] CommonDelimiters = new char[] {',','\t', '|' };

            string FileContents = null;
            using (StreamReader StreamReader = new StreamReader(this.Path))
            {
                FileContents = StreamReader.ReadToEnd();
            }
            int[] AppearanceCounts = CommonDelimiters.Select(Delimiter => FileContents.Count(f => f == Delimiter)).ToArray();

            if (AppearanceCounts[Array.IndexOf(CommonDelimiters, ',')] == AppearanceCounts.Max())
            {
                return true;
            } else
            {
                return false;
            }
        }

        private File2DArray SplitIntoBoxed2DArrayWithHeaders(string File)
        {
#warning deal with \r\n polluting stuff
            string RowSeparator = "\r\n";       
            char ColumnSeparator = ',';

            string[][] TwoDList;
            string[] ListOfLines = File.Split(RowSeparator.ToCharArray());

            TwoDList = ListOfLines.Select(x => x.Split(ColumnSeparator)).ToArray();

            File2DArray Return2DArray = new File2DArray();

            string[][] EmptyRows = (from string[] element in TwoDList
                                    where element.IsEmpty()
                                    select element).ToArray();

            List<string[]> RowstoRemove = new List<string[]>();
            RowstoRemove.AddRange(EmptyRows);

            if (ContainsHeaders)
            {
                RowstoRemove.Add(TwoDList[0]);  //Remove headers from content
                Return2DArray.Headers = TwoDList[0];
            }
            Return2DArray.Content = TwoDList.Except(RowstoRemove).ToArray();


            return Return2DArray;
        }



        public File2DArray SplitInto2DArray()
        {
            string FileContent = null;
            using (StreamReader reader = new StreamReader(this.Path))
            {
                FileContent = reader.ReadToEnd();
            }

            return SplitIntoBoxed2DArrayWithHeaders(FileContent);
        }
    }

    public static class StringArrayExtension
    {
        public static bool IsEmpty(this string[] Array)
        {
            if (Array.Length == 1 & Array[0] == "") { return true; }

            if (Array.All(element => element == Array[0])) { return true; }

            return false;
        }
    }
}
