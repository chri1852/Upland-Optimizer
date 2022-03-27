#region /* DLL IMPORTS */

# imports the Windows Forms and Drawing Functions
[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") 
[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms")

#endregion /* DLL IMPORTS */

# The global form object for the script
$GLOBAL:MAIN_FORM = $null

$GLOBAL:HTML_TEXTBOX = $null

$GLOBAL:HTML_VIEWBOX = $null

# Builds the form
function Build-Form()
{
    $labelFont = New-Object System.Drawing.Font("MS Sans Serif", 10)
    $labelFontBig = New-Object System.Drawing.Font("MS Sans Serif", 16)

    $mainForm = New-Object System.Windows.Forms.Form
    $mainForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Sizable
    $mainForm.KeyPreview = $true
    $mainForm.Size = New-Object System.Drawing.Size(720,920)
    $mainForm.Text = "WebViewer"
    $mainForm.Add_Resize({MainFormResizeFunction $_})

    $GLOBAL:HTML_TEXTBOX = New-Object System.Windows.Forms.TextBox
    $GLOBAL:HTML_TEXTBOX.Size = New-Object System.Drawing.Size(695,200)
    $GLOBAL:HTML_TEXTBOX.Location = New-Object System.Drawing.Point(5,5)
    $GLOBAL:HTML_TEXTBOX.Text = @"
<h2>My Newsfeed</h2>
<h4>My Subtitle One</h4>
<p>This is an informational sentence for this Newsfeed. This is a 
<b>bold</b> word, this is an <i>italic</i> word, and this is a 
<u>underlined</u> word.</p>
<h4>My Subtitle Two</h4>
<ul>
    <li>Red</li>
    <li>
    Yellow
    <ul>
        <li>Daisy</li>
        <li>Goldenrod</li>
        <li>Straw</li>
    </ul>
    </li>
    <li>Blue</li>
</ul>
"@
    $GLOBAL:HTML_TEXTBOX.Name = "focusTextBox"
    $GLOBAL:HTML_TEXTBOX.Multiline = $true
    $GLOBAL:HTML_TEXTBOX.add_TextChanged({textbox_textChanged $_})
    $mainForm.Controls.Add($GLOBAL:HTML_TEXTBOX)

    $GLOBAL:HTML_VIEWBOX = New-Object System.Windows.Forms.WebBrowser
    $GLOBAL:HTML_VIEWBOX.Size = New-Object System.Drawing.Size(695,705)
    $GLOBAL:HTML_VIEWBOX.Location = New-Object System.Drawing.Point(5,210)
    $GLOBAL:HTML_VIEWBOX.DocumentText = $GLOBAL:HTML_TEXTBOX.text
    $mainForm.Controls.Add($GLOBAL:HTML_VIEWBOX)

    return $mainForm
}

function textbox_textChanged($value)
{
    $GLOBAL:HTML_VIEWBOX.DocumentText = $GLOBAL:HTML_TEXTBOX.text
}

#region /* PRIMARY SCRIPT FUNCTION */

# The primary function for the script
function Run-PrimaryScript()
{
    $GLOBAL:MAIN_FORM.ShowDialog()
}

#endregion /* PRIMARY SCRIPT FUNCTION */

$GLOBAL:MAIN_FORM = Build-Form

Run-PrimaryScript