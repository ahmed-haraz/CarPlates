# CarPlates User Guide

## Getting Started

### Login
1. Launch the CarPlates app
2. Enter your username and password
3. Tap "Sign In"

### First-Time Setup
1. Go to Settings tab
2. Configure API URL if needed
3. Adjust OCR confidence threshold (default: 75%)
4. Enable/disable auto-resume scanning

## Scanning License Plates

### Automatic Scanning
1. Tap the "Scan" tab or "Scan License Plate" button on Dashboard
2. Point camera at the license plate
3. The app will automatically detect and recognize the plate
4. Vehicle information will appear in a bottom sheet
5. Tap anywhere to dismiss and continue scanning

### Manual Entry
1. On the Scanner screen, tap "Manual Entry"
2. Type the plate number
3. Tap "Search" to look up vehicle information

### Scanner Controls
- **Torch**: Toggle flashlight on/off
- **Switch Camera**: Toggle between front and back cameras
- **Pause/Resume**: Temporarily stop/start scanning

## Viewing History

### Search & Filter
- Use the search bar to find specific plates
- Tap the calendar icon to filter by date range
- Swipe left on any record to delete it

### Export
- Tap the export button to save history as CSV/JSON
- Files are saved to the device's Downloads folder

## Vehicle Details
- Tap any scan record to view full vehicle details
- See scan history for that specific plate
- Share vehicle information via other apps

## Settings

### Appearance
- **Dark Mode**: Toggle between light and dark themes
- **Language**: Switch between English and Arabic

### Scanner
- **OCR Confidence**: Adjust minimum confidence threshold (50-100%)
- **Auto Resume**: Automatically continue scanning after showing results

### API Configuration
- **API URL**: Change the backend server address
- **Sync Now**: Manually trigger pending uploads

### Data Management
- **Clear Cache**: Delete all local scan history
- **View Logs**: Access application logs for troubleshooting

## Offline Mode
The app works offline with these features:
- Scan and save plates locally
- Queue uploads for when connection is restored
- View cached vehicle information
- Automatic background sync when online

## Troubleshooting

### Camera not working
- Ensure camera permission is granted
- Check if another app is using the camera
- Restart the app

### OCR not recognizing plates
- Ensure good lighting conditions
- Hold the camera steady
- Adjust OCR confidence threshold in Settings
- Make sure the plate is clearly visible

### Sync failures
- Check internet connection
- Verify API URL in Settings
- Check if API server is accessible
- Review error logs for details
