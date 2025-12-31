use once_cell::sync::OnceCell;
use std::ffi::{c_char, c_int, CStr};
use std::ptr;
use tokenizers::Tokenizer;

static TOKENIZER: OnceCell<Tokenizer> = OnceCell::new();

/// Initialize the tokenizer from a tokenizer.json file path
/// Returns 0 on success, negative on error
#[no_mangle]
pub extern "C" fn tokenizer_initialize(path: *const c_char) -> c_int {
    if path.is_null() {
        return -1;
    }

    let c_str = unsafe { CStr::from_ptr(path) };
    let path_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return -2,
    };

    let tokenizer = match Tokenizer::from_file(path_str) {
        Ok(t) => t,
        Err(_) => return -3,
    };

    match TOKENIZER.set(tokenizer) {
        Ok(_) => 0,
        Err(_) => -4, // Already initialized
    }
}

/// Encode text to token IDs with special tokens added
/// Returns number of tokens on success, negative on error
#[no_mangle]
pub extern "C" fn tokenizer_encode(
    text: *const c_char,
    out_ids: *mut c_int,
    max_len: usize,
) -> c_int {
    if text.is_null() || out_ids.is_null() {
        return -1;
    }

    let c_str = unsafe { CStr::from_ptr(text) };
    let text_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return -2,
    };

    let tokenizer = match TOKENIZER.get() {
        Some(t) => t,
        None => return -3, // Not initialized
    };

    // Encode with add_special_tokens = true (CRITICAL!)
    let encoding = match tokenizer.encode(text_str, true) {
        Ok(enc) => enc,
        Err(_) => return -4,
    };

    let ids = encoding.get_ids();
    let len = ids.len().min(max_len);

    unsafe {
        for i in 0..len {
            *out_ids.add(i) = ids[i] as c_int;
        }
    }

    len as c_int
}

/// Free the tokenizer (optional cleanup)
#[no_mangle]
pub extern "C" fn tokenizer_free() {
    // OnceCell doesn't support clearing, but we can just leave it
    // The tokenizer will be freed when the DLL is unloaded
}
