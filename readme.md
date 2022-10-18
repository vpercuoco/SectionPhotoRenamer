# SectionPhotoRenamer


Use this tool to change the names of images arriving post expedition. All images should have .tif extensions. They consist of section linescans (LSIMG), whole round quadrant images (WRLS), and whole round composite images (WRLSC). Occasionally, closeups, testing images, and other types are saved alongside the section images. Place these in a "junk" folder but do not get rid of them.

SectionPhotoRenamer uses Regex to parse filenames and identify the analysis they represent.
The current regex string:

```regex
(?<expedition>[0-9a-z]+)-(?<site>[a-z][0-9]+[a-z])-(?<core>[0-9]+[a-z])-(?<section>[0-9]+|cc)-?(?<half>[a|w])?_?(?<textid>[a-z]+[0-9]+)_?(?<datetime>([0-9]+)(_)?(?<degree>[0-9]+)?)?(_WRLS|_WRLSC)?(?<extension>.tif)
```

Solves for names like: 
```
LSIMG:
EXP397T_64072951_999-u9999a-1h-1-a_shlf11239051_20220914121334.tif
EXP397T_64073721_397t-u1584a-3r-1-a_shlf11708131_20220916071808.tif
EXP397T_64074321_397t-u1584a-4r-1-a_shlf11708171_20220916124424.tif
EXP397T_64074571_397t-u1584a-4r-2-a_shlf11708201_20220916131215.tif

WRLS:
EXP397T_64152601_397t-u1585a-31r-8_sect11716601_20220921203439_0.tif
EXP397T_64152641_397t-u1585a-31r-8_sect11716601_20220921203530_90.tif
EXP397T_64152681_397t-u1585a-31r-8_sect11716601_20220921203619_180.tif
EXP397T_64152721_397t-u1585a-31r-8_sect11716601_20220921203658_270.tif

WRLSC:
EXP397T_64153721_397t-u1585a-27r-7_sect11715731_WRLS.tif
EXP397T_64153771_397t-u1585a-14r-4_sect11711501_WRLS.tif
EXP397T_64153781_397t-u1585a-14r-1_sect11711411_WRLS.tif

```

Change the regex string in the program when need be. A good website for testing regex against test strings is: https://regexr.com/

Use Bulk Rename Utility instead of SectionPhotoRenamer if greater customization is needed.

## Installing

Open solution in Visual Studio. Click Build > Build Solution. The solution may be compiled and deployed as an executable but it is most straightforward to run it straight from Visual Studio.

## Running

If running from Visual Studio, in the DEBUG region of Program.Main() specify input parameters as in the example below. It is best to run the program once with moveFiles and processChecksum equal to false. In the output directory a "change_names.csv" file will be placed. Review this file to ensure the names will be changed as expected. 

If the names appear fine, then rerun the application with processChecksums and moveFiles set to true. The app determines filehashes in this step and will take much longer to run than before. Afterwards, the "changed_names.csv" file should include three completed columns: original_name, new_name, and checksum. The tif files will be moved and organized within the outputDirectory according to site then image file type.

#### From Visual Studio
```csharp
#if DEBUG  // Keeping this section here in case code needs to be run as a script
            string inputDirectory = @"D:\to_be_added\EXP397T-TIF\";
            string logFile = @"D:\to_be_added\EXP397T_TIFF_NAME_CHANGES.txt";
            string outputDirectory = @"D:\to_be_added\organized\";
            bool moveFiles = false;
            bool processChecksum = false;
#else
...
#endif
```

#### From the command prompt

Example input of running via the command prompt
```shell
C:\users\user1\my_executable_here\sectionphotorenamer "D:\to_be_added\EXP397T-TIF\" "D:\to_be_added\EXP397T_TIFF_NAME_CHANGES.txt" "D:\to_be_added\organized\" false false
```

### Example of output directory:

The subfolder "junk" must be added manually, and should consist of the files whose names could not be parsed. They will still reside in the original folder.

```
+---u9999a
|   +---core_photos
|   |       999_u9999a_1h_1.tif
|   |
|   +---wrls
|   \---wrlsc
+---u1584a
|   +---core_photos
|   |       397t_u1584a_3r_1.tif
|   |       397t_u1584a_4r_1.tif
|   |       397t_u1584a_4r_2.tif
|   |
|   +---wrls
|   \---wrlsc
+---u1585a
|   +---core_photos
|   |       397t_u1585a_3r_1.tif
|   |       397t_u1585a_3r_cc.tif
|   |       397t_u1585a_4r_1.tif
|   |       397t_u1585a_4r_2.tif
|   |       397t_u1585a_4r_3.tif
|   |       397t_u1585a_39r_1.tif
|   |       397t_u1585a_38r_9.tif
|   |
|   +---wrls
|   |       wrls_397t_u1585a_14r_1_0.tif
|   |       wrls_397t_u1585a_14r_1_90.tif
|   |       wrls_397t_u1585a_14r_1_180.tif
|   |       wrls_397t_u1585a_14r_1_270.tif
|   |       wrls_397t_u1585a_14r_2_0.tif
|   |       wrls_397t_u1585a_14r_2_90.tif
|   |       wrls_397t_u1585a_14r_2_180.tif
|   |
|   \---wrlsc
|           wrlsc_397t_u1585a_27r_7.tif
|           wrlsc_397t_u1585a_14r_4.tif
|           wrlsc_397t_u1585a_14r_1.tif
|           wrlsc_397t_u1585a_14r_2.tif
|
\---junk
        EXP397T_64072911_999_U9397A_1X_1_0-3cm-Dry.tif
        EXP397T_64072931_999_U9397A_1X_CC_0-3cm-Dry.tif
        EXP397T_64223881_397T_U1585A_35R_9A_37-48cm-Dry.tif
```
