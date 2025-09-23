// Returns the name of the first selected file from an input[type=file] element
export function getSelectedFileName(input) {
  if (!input || !input.files || input.files.length === 0) return "";
  return input.files[0].name || "";
}
// ES module export for Blazor dynamic import
export async function readFileText(inputRef){
  var input = inputRef instanceof HTMLElement ? inputRef : document.querySelector('input[type=file]');
  if (!input || !input.files || !input.files[0]) return '';
  return await new Promise(function(resolve){
    var reader = new FileReader();
    reader.onload = function(e){ resolve(e.target.result); };
    reader.onerror = function(){ resolve(''); };
    reader.readAsText(input.files[0]);
  });
}

export function triggerFileInput(inputRef) {
  if (inputRef instanceof HTMLElement) {
    inputRef.click();
  } else {
    var input = document.querySelector('input[type=file]');
    if (input) input.click();
  }
}
