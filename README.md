# Microsoft Business Application & Power Platform development samples
This repository contains development samples for Microsoft PowerApps/Flow, CDS and Azure

## Business Card Scanner

This is an end2end sample using Microsoft PowerApps, Microsoft Flow and Azure Functions as well as Azure Cognitive Services to do a mobile business card scan and turn the image into a contact in Microsoft Dynamics 365.

## AI Damage analyzer

End2End sample application to do a mobile damage analysis with PowerApps and Azure Custom Vision. The app allows you to snap a damaged device (cell phone displays in the sample) and analyse the type of damage. The QR code reader reads an asset tag and searches for the asset in Dynamics 365. When all data is captured the app kicks off a Microsoft Flow that creates a customer service case with the right product for the identified type of damage associated and the account and scanned customer asset. 
