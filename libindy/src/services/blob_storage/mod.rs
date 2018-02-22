pub mod default_writer;
pub mod default_reader;

extern crate digest;
extern crate indy_crypto;
extern crate sha2;

use self::digest::{FixedOutput, Input};
use self::indy_crypto::cl::RevocationTailsGenerator;
use self::indy_crypto::cl::Tail;

use errors::common::CommonError;

use std::cell::RefCell;
use std::collections::HashMap;

const TAILS_BLOB_TAG_SZ: usize = 2;
const TAIL_SIZE: usize = Tail::BYTES_REPR_SIZE;

trait TailsWriterType {
    fn create(&self, config: &str) -> Result<Box<TailsWriter>, CommonError>;
}

trait TailsWriter {
    fn append(&mut self, bytes: &[u8]) -> Result<usize, CommonError>;
    fn finalize(&mut self, hash: &[u8]) -> Result<String, CommonError>;
}

trait TailsReaderType {
    fn open(&self, config: &str) -> Box<TailsReader>;
}

trait TailsReader {
    fn verify(&self) -> ();
    fn close(&self) -> ();
    fn read(&self, size: usize, offset: usize) -> Vec<u8>;
}

pub struct TailsService {
    writer_types: RefCell<HashMap<String, Box<TailsWriterType>>>,

    reader_types: RefCell<HashMap<String, Box<TailsReaderType>>>,
    readers: RefCell<HashMap<u32, Box<TailsReader>>>,
}

impl TailsService {
    pub fn new() -> TailsService {
        let mut writer_types: HashMap<String, Box<TailsWriterType>> = HashMap::new();
        writer_types.insert("default".to_owned(), Box::new(default_writer::DefaultTailsWriterType::new()));
        let mut reader_types: HashMap<String, Box<TailsReaderType>> = HashMap::new();
        reader_types.insert("default".to_owned(), Box::new(default_reader::DefaultTailsReaderType::new()));

        TailsService {
            writer_types: RefCell::new(writer_types),

            reader_types: RefCell::new(reader_types),
            readers: RefCell::new(HashMap::new()),
        }
    }
}

/* Writer part */
impl TailsService {
    pub fn store_tails_from_generator(&self,
                                      type_: &str,
                                      config: &str,
                                      rtg: &mut RevocationTailsGenerator)
                                      -> Result<String, CommonError> {
        let mut tails_writer = self.writer_types.try_borrow()?
            .get(type_).unwrap().create(config)?; //FIXME UnknownType error instead of unwrap
        let mut hasher = sha2::Sha256::default();

        //FIXME store version/tag/meta at start of the Tail's BLOB

        while let Some(tail) = rtg.next()? {
            let tail_bytes = tail.to_bytes()?;
            hasher.process(tail_bytes.as_slice());
            tails_writer.append(tail_bytes.as_slice())?;
        }

        tails_writer.finalize(hasher.fixed_result().as_slice())
    }
}

/* Reader part */
impl TailsService {
    pub fn read(&self, handle: u32, idx: usize) -> Tail {
        let bytes = self.readers.borrow().get(&handle).unwrap()
            .read(TAIL_SIZE, TAILS_BLOB_TAG_SZ + TAIL_SIZE * idx);
        Tail::from_bytes(bytes.as_slice()).unwrap()
    }
}