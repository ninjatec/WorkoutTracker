/**
 * TinyMCE initialization for blog editing
 */
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        if (document.querySelector('#Content')) {
            tinymce.init({
                selector: '#Content',
                plugins: 'image link lists table code codesample autolink autoresize preview media pagebreak wordcount',
                toolbar: 'undo redo | blocks | bold italic | alignleft aligncenter alignright | bullist numlist outdent indent | link image media table | code codesample | preview',
                menubar: 'file edit view insert format tools table help',
                toolbar_mode: 'sliding',
                height: 500,
                model: 'dom',
                autoresize_bottom_margin: 50,
                image_title: true,
                automatic_uploads: true,
                file_picker_types: 'image',
                file_picker_callback: function (cb, value, meta) {
                    // Create a file input element for image uploads
                    var input = document.createElement('input');
                    input.setAttribute('type', 'file');
                    input.setAttribute('accept', 'image/*');

                    input.onchange = function () {
                        var file = this.files[0];
                        var reader = new FileReader();
                        reader.onload = function () {
                            // Create a FormData instance and append the file
                            var formData = new FormData();
                            formData.append('image', file);

                            // Send the file to the server
                            fetch('/api/blog/upload-image', {
                                method: 'POST',
                                body: formData,
                                headers: {
                                    'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]').value
                                }
                            })
                                .then(response => {
                                    if (!response.ok) {
                                        throw new Error('Network response was not ok');
                                    }
                                    return response.json();
                                })
                                .then(data => {
                                    // Insert the uploaded image to the editor
                                    if (data.success) {
                                        cb(data.imageUrl, { title: file.name });
                                    } else {
                                        console.error('Upload error:', data.message);
                                        alert('Image upload failed: ' + data.message);
                                    }
                                })
                                .catch(error => {
                                    console.error('Error uploading image:', error);
                                    alert('Error uploading image: ' + error.message);
                                });
                        };
                        reader.readAsDataURL(file);
                    };

                    input.click();
                },
                content_style: 'body { font-family:Helvetica,Arial,sans-serif; font-size:16px }',
                promotion: false
            });
        }
    });
})();
