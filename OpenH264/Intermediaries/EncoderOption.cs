namespace OpenH264.Intermediaries;
public enum EncoderOption {
    ENCODER_OPTION_DATAFORMAT = 0,
    ENCODER_OPTION_IDR_INTERVAL,               ///< IDR period,0/-1 means no Intra period (only the first frame); lager than 0 means the desired IDR period, must be multiple of (2^temporal_layer)
    ENCODER_OPTION_SVC_ENCODE_PARAM_BASE,      ///< structure of Base Param
    ENCODER_OPTION_SVC_ENCODE_PARAM_EXT,       ///< structure of Extension Param
    ENCODER_OPTION_FRAME_RATE,                 ///< maximal input frame rate, current supported range: MAX_FRAME_RATE = 30,MIN_FRAME_RATE = 1
    ENCODER_OPTION_BITRATE,
    ENCODER_OPTION_MAX_BITRATE,
    ENCODER_OPTION_INTER_SPATIAL_PRED,
    ENCODER_OPTION_RC_MODE,
    ENCODER_OPTION_RC_FRAME_SKIP,
    ENCODER_PADDING_PADDING,                   // 0:disable padding;1:padding

    ENCODER_OPTION_PROFILE,                    // assgin the profile for each layer
    ENCODER_OPTION_LEVEL,                      // assgin the level for each layer
    ENCODER_OPTION_NUMBER_REF,                 // the number of refererence frame
    ENCODER_OPTION_DELIVERY_STATUS,            // the delivery info which is a feedback from app level

    ENCODER_LTR_RECOVERY_REQUEST,
    ENCODER_LTR_MARKING_FEEDBACK,
    ENCODER_LTR_MARKING_PERIOD,
    ENCODER_OPTION_LTR,                        // 0:disable LTR;larger than 0 enable LTR; LTR number is fixed to be 2 in current encoder
    ENCODER_OPTION_COMPLEXITY,

    ENCODER_OPTION_ENABLE_SSEI,                // enable SSEI: true--enable ssei; false--disable ssei
    ENCODER_OPTION_ENABLE_PREFIX_NAL_ADDING,   // enable prefix: true--enable prefix; false--disable prefix
    ENCODER_OPTION_SPS_PPS_ID_STRATEGY,        // different stategy in adjust ID in SPS/PPS: 0- constant ID, 1-additional ID, 6-mapping and additional

    ENCODER_OPTION_CURRENT_PATH,
    ENCODER_OPTION_DUMP_FILE,                  // dump layer reconstruct frame to a specified file
    ENCODER_OPTION_TRACE_LEVEL,                // trace info based on the trace level
    ENCODER_OPTION_TRACE_CALLBACK,             // a void (*)(void* context, int level, const char* message) function which receives log messages
    ENCODER_OPTION_TRACE_CALLBACK_CONTEXT,     // context info of trace callback

    ENCODER_OPTION_GET_STATISTICS,             // read only
    ENCODER_OPTION_STATISTICS_LOG_INTERVAL,    // log interval in millisecond

    ENCODER_OPTION_IS_LOSSLESS_LINK,           // advanced algorithmetic settings

    ENCODER_OPTION_BITS_VARY_PERCENTAGE        // bit vary percentage
}