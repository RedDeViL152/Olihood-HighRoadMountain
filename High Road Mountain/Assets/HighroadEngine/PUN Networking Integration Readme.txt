The Highroad Engine comes with a PUN 2 ready sample
Make sure you read the description below for a smooth experience

Install PUN 2
---------------------------

Be careful not to install PUN 1, Highroad Engine supports only PUN 2.
You can get PUN 2 from the Asset store: https://assetstore.unity.com/packages/tools/network/pun-2-free-119922



WHAT AM I SUPPOSED TO DO WITH IT ?
----------------------------------

Once you have installed PUN 2, you need to setup an APPID with Pun, on their online dashboard


I DON'T WANT TO USE PUN, HOW DO I CLEAN UP THE PROJECT
----------------------------------

To remove PUN 2 completly from the project, you need to do the following:

- delete the "Photon" folder at the root of your Project's asset.
- go to your Player Settings, in the configuration section and in the 'Scripting Define Symbol' field, remove the following PHOTON_UNITY_NETWORKING;PUN_2_0_OR_NEWER;PUN_2_OR_NEWER

you should only be left with "CROSS_PLATFORM_INPUT" in your 'Scripting Define Symbol' field.
